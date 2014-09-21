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

    class Wiadomosciownia
    {        
        Dictionary<string, Queue<byte[]>> komunikatyWychodzace = new Dictionary<string, Queue<byte[]>>();
        Dictionary<string, Boolean> wysylanieWToku = new Dictionary<string, Boolean>();
        Dictionary<string, object> zamkiWysylania = new Dictionary<string, object>();

        Buforownia buforownia = new Buforownia(512);
        CzytanieSkonczone czytanieSkonczone;
        const int DlugoscNaglowka = 5; 

        public Wiadomosciownia(CzytanieSkonczone czytanieSkonczone)
        {
            this.czytanieSkonczone = czytanieSkonczone;
        }

        public event NowaWiadomosc NowaWiadomosc;

        public void CzytajZawartosc(Stream strumien, string guidStrumienia ,string id, TypWiadomosci rodzaj, int dlugoscWiadomosci)
        {
            if (dlugoscWiadomosci == 0) { czytanieSkonczone(id); }
            czytajZawartosc(strumien, guidStrumienia , id, rodzaj, dlugoscWiadomosci, 0);    
        }           
        
        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(Stream strumien, String idRozmowcy, byte[] komunikat)
        {
            try
            {
                //Trace.TraceInformation(String.Format("Probojemy wyslac wiadomosc rodzaj {0}: {1} ", rodzaj, wiadomosc));
                //if (komunikat.Length  && rodzaj != Protokol.DajOpis) { return;  }
                // tranformacja tekstu w bajty
                //Trace.TraceInformation(String.Format("Wysylamy wiadomosc rodzaj {0}: {1} ",rodzaj, wiadomosc));

                // wysylanie bajtow polaczeniem TCP 
                dajKolejkeWiadomosci(idRozmowcy).Enqueue(komunikat);
                wysylajZKolejki(strumien, idRozmowcy);
            }
            catch(Exception ex) 
            {
                Trace.TraceInformation("[WyslijWiadomosc]" + ex.ToString());
            }
        }

        public void DodajUzytkownika(string id)
        {
            wysylanieWToku.Add(id, false);
            zamkiWysylania.Add(id, new object());
        }

        public void UsunUzytkownika(string idUzytkownika)
        {
            zamkiWysylania.Remove(idUzytkownika);
            wysylanieWToku.Remove(idUzytkownika);
            buforownia.Usun(idUzytkownika);
        }



        Queue<byte[]> dajKolejkeWiadomosci(string id)
        {
            if (!komunikatyWychodzace.ContainsKey(id))
            {
                komunikatyWychodzace.Add(id, new Queue<byte[]>());
            }
            return komunikatyWychodzace[id];
        }
        
        void wysylajZKolejki(Stream strumien, string idUzytkownika)
        {
            byte[] doWyslania = null;
            lock (zamkiWysylania[idUzytkownika])
            {
                if (wysylanieWToku[idUzytkownika]){ return; }

                if (dajKolejkeWiadomosci(idUzytkownika).Any())
                {
                    doWyslania = dajKolejkeWiadomosci(idUzytkownika).Dequeue();
                    wysylanieWToku[idUzytkownika] = true;
                }
            }

            if (doWyslania != null)
            {
                strumien.BeginWrite(doWyslania, 0, 
                    doWyslania.Length, new AsyncCallback(komunikatWyslany), new WyslijKomunikatStatus() 
                    { IdNadawcy = idUzytkownika, Strumien = strumien });
            }
        }

        void komunikatWyslany(IAsyncResult wynik)
        {
            var status = (WyslijKomunikatStatus)wynik.AsyncState;
            status.Strumien.EndWrite(wynik);
            Trace.TraceInformation(String.Format("Komunikat wyslany"));
            lock (zamkiWysylania[status.IdNadawcy])
            { wysylanieWToku[status.IdNadawcy] = false; }
            wysylajZKolejki(status.Strumien, status.IdNadawcy);
        }


        void czytajZawartosc(Stream strumien, string guidStrumienia ,string id, TypWiadomosci rodzaj, int dlugoscWiadomosci, int wczytano)
        {
            strumien.BeginRead(buforownia[id], wczytano,
                dlugoscWiadomosci, new AsyncCallback(zawartoscWczytana),
                new CzytajWiadomoscStatus() { IdNadawcy = id, Rodzaj = rodzaj, 
                    DlugoscWiadomosci = dlugoscWiadomosci, Wczytano = wczytano, Strumien = strumien, guidStrumienia= guidStrumienia });
        }

        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void zawartoscWczytana(IAsyncResult wynik)
        {
            // od kogo przyszla wiadomosc
            var status = (CzytajWiadomoscStatus)wynik.AsyncState;
            // zakoncz operacje asynchroniczna
            var strumien = status.Strumien;
            
            try
            {
                int bajtyWczytane = strumien.EndRead(wynik);
                if (status.DlugoscWiadomosci > status.Wczytano + bajtyWczytane)
                { 
                    czytajZawartosc(strumien, status.guidStrumienia, status.IdNadawcy, status.Rodzaj, 
                        status.DlugoscWiadomosci, status.Wczytano + bajtyWczytane); 
                }
            }
            catch (Exception ex) {
                Trace.TraceInformation(ex.ToString());
            }
            // dekodujemy wiadomosc
            string wiadomosc = Encoding.UTF8.GetString(buforownia[status.IdNadawcy], 0, status.DlugoscWiadomosci);

            // czyscimy bufor
            Array.Clear(buforownia[status.IdNadawcy], 0, status.DlugoscWiadomosci);
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if (NowaWiadomosc != null) // informujemy zainteresowanych
            { NowaWiadomosc(status.IdNadawcy, status.Rodzaj, wiadomosc); }

            czytanieSkonczone(status.guidStrumienia);
        }
        class WyslijKomunikatStatus
        {
            public String IdNadawcy { get; set; }
            public Stream Strumien{ get; set; }
        }

        class CzytajWiadomoscStatus
        {
            public Stream Strumien { get; set; }
            public String guidStrumienia{ get; set; }
            public String IdNadawcy { get; set; }
            public TypWiadomosci Rodzaj { get; set; }
            public int DlugoscWiadomosci { get; set; }
            public int Wczytano { get; set; }
        }

    }

    public enum TypWiadomosci
    {
        Zwykla,
        Opis
    }
}
