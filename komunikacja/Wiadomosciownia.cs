#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    /// <summary>
    /// Obiekt odpowiedziany za przesylanie i odbior komunikatow tekstowych
    /// </summary>
    class Wiadomosciownia
    {
        // dla kazdego uzytkownika kolejka wiadomosci do wyslania
        Dictionary<string, Queue<byte[]>> komunikatyWychodzace = new Dictionary<string, Queue<byte[]>>();

        // zeby uniknac wysylania przez jeden strumien z roznych watkow uzywamy
        // nastepnych dwoch obiektow
        Dictionary<string, Boolean> wysylanieWToku = new Dictionary<string, Boolean>();
        Dictionary<string, object> zamkiWysylania = new Dictionary<string, object>();

        // obiekt zajmujacy sie alokacja buforow dla odbieranych wiadomosci
        Buforownia buforownia = new Buforownia(512);

        // uruchamiamy te delegate, gdy skonczylismy czytac ze strumienia
        CzytanieSkonczone czytanieSkonczone;

        // dlugosc (w bajtach) naglowka komunikatu
        const int DlugoscNaglowka = 5;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="czytanieSkonczone">uruchamiamy te delegate, gdy skonczylismy czytac ze strumienia</param>
        public Wiadomosciownia(CzytanieSkonczone czytanieSkonczone)
        { this.czytanieSkonczone = czytanieSkonczone; }

        /// <summary>
        /// nadeszla nowa wiadomosc tekstowa od innego uzytkownika
        /// </summary>
        public event NowaWiadomosc NowaWiadomosc;

        /// <summary>
        /// Czytaj ze strumienia zawartosc wiadomosci (juz bez naglowka)
        /// </summary>
        /// <param name="strumien">stad czytamy</param>
        /// <param name="idStrumienia">identyfikator strumienia</param>
        /// <param name="idUzytkownika">od kogo ta wiadomosc</param>
        /// <param name="rodzaj">Zwykla / Opis</param>
        /// <param name="dlugoscWiadomosci">ile bajtow do wczytania</param>
        public void CzytajZawartosc(Stream strumien, string idStrumienia,
            string idUzytkownika, TypWiadomosci rodzaj, int dlugoscWiadomosci)
        {
            if (dlugoscWiadomosci == 0) { 
                czytanieSkonczone(idStrumienia, "CzytajZawartosc");
                return;
            }
            czytajZawartosc(strumien, idStrumienia, idUzytkownika, rodzaj, dlugoscWiadomosci, 0);
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="strumien">dokad piszemy</param>
        /// <param name="idRozmowcy">do kogo piszemy</param>
        /// <param name="komunikat">wiadomosc z naglowkiem</param>
        public void WyslijWiadomosc(Stream strumien, String idRozmowcy, byte[] komunikat)
        {
            try
            {
                Trace.TraceInformation("Wiadomosciownia.WyslijWiadomosc " + komunikat[0].ToString());
                if (strumien == null) { return; }
                // wysylanie bajtow polaczeniem TCP 
                dajKolejkeWiadomosci(idRozmowcy).Enqueue(komunikat);
                wysylajZKolejki(strumien, idRozmowcy);
            }
            catch (Exception ex)
            { }
        }

        /// <summary>
        /// Obslugujemy nowego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void DodajUzytkownika(string idUzytkownika)
        {
            wysylanieWToku.Add(idUzytkownika, false);
            zamkiWysylania.Add(idUzytkownika, new object());
        }

        /// <summary>
        /// Tego uzytkownika juz nie obslugujemy
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void UsunUzytkownika(string idUzytkownika)
        {
            zamkiWysylania.Remove(idUzytkownika);
            wysylanieWToku.Remove(idUzytkownika);
            buforownia.Usun(idUzytkownika);
        }

        // daj kolejke wiadomosci do wyslania do danego uzytkownika
        Queue<byte[]> dajKolejkeWiadomosci(string idUzytkownika)
        {
            if (!komunikatyWychodzace.ContainsKey(idUzytkownika))
            {
                komunikatyWychodzace.Add(idUzytkownika, new Queue<byte[]>());
            }
            return komunikatyWychodzace[idUzytkownika];
        }

        // zacznij wysylac komunikaty z kolejki
        void wysylajZKolejki(Stream strumien, string idUzytkownika)
        {
            byte[] doWyslania = null;
            lock (zamkiWysylania[idUzytkownika])
            {
                if (wysylanieWToku[idUzytkownika]) { return; }

                if (dajKolejkeWiadomosci(idUzytkownika).Any())
                {
                    doWyslania = dajKolejkeWiadomosci(idUzytkownika).Dequeue();
                    wysylanieWToku[idUzytkownika] = true;
                }
            }

            if (doWyslania == null) { return; }

            strumien.BeginWrite(doWyslania, 0,
                doWyslania.Length, new AsyncCallback(komunikatWyslany), new WyslijKomunikatStatus() { IdNadawcy = idUzytkownika, Strumien = strumien });

        }

        // komunikat zostal wyslany
        void komunikatWyslany(IAsyncResult wynik)
        {
            var status = (WyslijKomunikatStatus)wynik.AsyncState;
            status.Strumien.EndWrite(wynik);
            lock (zamkiWysylania[status.IdNadawcy])
            { wysylanieWToku[status.IdNadawcy] = false; }
            wysylajZKolejki(status.Strumien, status.IdNadawcy);
        }

        // czytaj ze strumienia
        void czytajZawartosc(Stream strumien, string idStrumienia,
            string idUzytkownika, TypWiadomosci rodzaj, int dlugoscWiadomosci, int wczytanoBajow)
        {
            strumien.BeginRead(buforownia[idUzytkownika], wczytanoBajow,
                dlugoscWiadomosci, new AsyncCallback(zawartoscWczytana),
                new CzytajWiadomoscStatus()
                {
                    IdNadawcy = idUzytkownika,
                    Rodzaj = rodzaj,
                    DlugoscWiadomosci = dlugoscWiadomosci,
                    Wczytano = wczytanoBajow,
                    Strumien = strumien,
                    IdStrumienia = idStrumienia
                });
        }

        // wczytalismy cos ze strumienia
        void zawartoscWczytana(IAsyncResult wynik)
        {
            var status = (CzytajWiadomoscStatus)wynik.AsyncState;

            try
            {
                int bajtyWczytane = status.Strumien.EndRead(wynik);
                if (status.DlugoscWiadomosci > status.Wczytano + bajtyWczytane)
                {
                    czytajZawartosc(status.Strumien, status.IdStrumienia, status.IdNadawcy, status.Rodzaj,
                        status.DlugoscWiadomosci, status.Wczytano + bajtyWczytane);
                    return;
                }
            }
            catch (Exception ex) { }
            
            // dekodujemy wiadomosc
            string wiadomosc = Encoding.UTF8.GetString(buforownia[status.IdNadawcy], 0, status.DlugoscWiadomosci);

            // czyscimy bufor
            Array.Clear(buforownia[status.IdNadawcy], 0, status.DlugoscWiadomosci);
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if (NowaWiadomosc != null) // informujemy zainteresowanych
            { NowaWiadomosc(status.IdNadawcy, status.Rodzaj, wiadomosc); }
            czytanieSkonczone(status.IdStrumienia, "zawartosWczytana");
        }

        // obiekt uzywany w operacji asynchronicznej
        class WyslijKomunikatStatus
        {
            public String IdNadawcy { get; set; }
            public Stream Strumien { get; set; }
        }

        // obiekt uzywany w operacji asynchronicznej
        class CzytajWiadomoscStatus
        {
            public Stream Strumien { get; set; }
            public String IdStrumienia { get; set; }
            public String IdNadawcy { get; set; }
            public TypWiadomosci Rodzaj { get; set; }
            public int DlugoscWiadomosci { get; set; }
            public int Wczytano { get; set; }
        }
    }

    /// <summary>
    /// Rodzaj wiadomosci tekstowej
    /// </summary>
    public enum TypWiadomosci
    {
        Zwykla,
        Opis
    }
}
