#define TRACE

using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    public delegate void OtwartoPolaczenieZasadnicze(string idUzytkownika);
    public delegate void ZamknietoPolaczenieZasadnicze(string idUzytkownika);
    public delegate void CzytanieSkonczone(string idUzytkownika);
    
    /// <summary>
    /// Obiekt odpowiedzialny za koordynacje przesylania i odbierania komunikatow
    /// </summary>
    class Protokol
    {
        const byte KoniecPolaczenia = 0;
        const byte ZwyklaWiadomosc = 1;
        const byte DajOpis = 2;
        const byte WezOpis = 3;
        const byte WezPlik = 4;
        const byte DajPlik = 5;

        Wiadomosciownia wiadomosciownia;
        Plikownia plikownia;
        Ustawienia ustawienia;
        Mapownik mapownik;
        Strumieniownia strumieniownia;

        const int DlugoscNaglowka = 5; // 1 bajt na rodzaj komunikatu, 4 na dlugosc
        
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mapownik">obiekt mapownika</param>
        /// <param name="ustawienia">obiekt ustawien</param>
        public Protokol(Mapownik mapownik, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(new CzytanieSkonczone(czekajNaZapytanie)); ;
            
            foreach (var i in mapownik.WszystkieId) { wiadomosciownia.DodajUzytkownika(i); }

            this.ustawienia = ustawienia;
            this.mapownik = mapownik;
            
            strumieniownia = new Strumieniownia(mapownik, ustawienia);
            strumieniownia.GotowyDoOdbioru += strumieniownia_GotowyDoOdbioru;
            this.plikownia = new Plikownia();
        }     

        /// <summary>
        /// Nadeszla nowa wiadomosc tekstowa
        /// </summary>
        public event NowaWiadomosc NowaWiadomosc {
            add { wiadomosciownia.NowaWiadomosc += value; }
            remove { wiadomosciownia.NowaWiadomosc -= value; }
        }

        /// <summary>
        /// Polaczylismy sie z innym uzytkownikiem
        /// </summary>
        public event OtwartoPolaczenieZasadnicze OtwartoPolaczenieZasadnicze
        {
            add { strumieniownia.OtwartoPolaczenieZasadnicze += value; }
            remove { strumieniownia.OtwartoPolaczenieZasadnicze -= value; }
        }

        /// <summary>
        /// Rozlaczylismy sie z innym uzytkownikiem
        /// </summary>
        public event ZamknietoPolaczenieZasadnicze ZamknietoPolaczenieZasadnicze 
        {
            add { strumieniownia.ZamknietoPolaczenieZasadnicze += value; }
            remove { strumieniownia.ZamknietoPolaczenieZasadnicze -= value; }
        }

        /// <summary>
        /// Polacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void Polacz(string idUzytkownika) { strumieniownia.Polacz(idUzytkownika); }

        /// <summary>
        /// Rozlacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void Rozlacz(string idUzytkownika) { strumieniownia.RozlaczUzytkownika(idUzytkownika); }

        /// <summary>
        /// Rozpocznij dzialanie protokolu
        /// </summary>
        public void Start() { strumieniownia.Start(); }

        /// <summary>
        /// Zatrzymaj dzialanie protokolu
        /// </summary>
        public void Stop() { strumieniownia.Stop(); }

        /// <summary>
        /// Obsluguj nowego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void DodajUzytkownika(string idUzytkownika) 
        { wiadomosciownia.DodajUzytkownika(idUzytkownika); }

        /// <summary>
        /// Nie obsluguj juz tego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void UsunUzytkownika(string idUzytkownika)
        { wiadomosciownia.UsunUzytkownika(idUzytkownika); }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="wiadomosc">tekst wiadomosci</param>
        public void WyslijWiadomosc(String idUzytkownika, String wiadomosc)
        { wyslijWiadomosc(idUzytkownika, ZwyklaWiadomosc, wiadomosc); }

        /// <summary>
        /// Wyslij swoj opis
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="opis">tekst opisu</param>
        public void WyslijOpis(String idUzytkownika, String opis)
        { wyslijWiadomosc(idUzytkownika, WezOpis, opis); }

        /// <summary>
        /// Popros innego uzytkownika o jego opis
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void PoprosOpis(String idUzytkownika)
        { wyslijWiadomosc(idUzytkownika, DajOpis, ""); }

        /// <summary>
        /// Wyslij plik to innego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="sciezka">sciezka do pliku</param>
        public void WyslijPlik(String idUzytkownika, String sciezka)
        { plikownia.Wyslij(idUzytkownika, sciezka); }

        // wyslij wiadomosc tekstowa
        void wyslijWiadomosc(String idRozmowcy, byte rodzaj, String wiadomosc)
        {
            wiadomosciownia.WyslijWiadomosc(strumieniownia.DajStrumienZasadniczy(idRozmowcy)
              , idRozmowcy, stworzKomunikat(rodzaj, wiadomosc));
        }
        
        // Czekaj (pasywnie) na zapytania i wiadomosci
        void czekajNaZapytanie(string idStrumienia)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
            var polaczenie = strumieniownia.DajPolaczenie(idStrumienia); 
            var wynik = new StatusObsluzZapytanie() { IdStrumienia = idStrumienia, Naglowek = new byte[DlugoscNaglowka] };
            polaczenie.Strumien.BeginRead(wynik.Naglowek, 0, DlugoscNaglowka, obsluzZapytanie, wynik);
        }

        // przyszlo nowe zapytanie / wiadomosc
        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.Naglowek[0].ToString());
            int dlugoscWiadomosci = BitConverter.ToInt32(status.Naglowek, 1);
            if (!strumieniownia.CzyZnasz(status.IdStrumienia)) { return; }
            var polaczenie = strumieniownia.DajPolaczenie(status.IdStrumienia);

            try { polaczenie.Strumien.EndRead(wynik); }
            catch // zostalismy rozlaczeni
            {  strumieniownia.ToPolaczenieNieDziala(status.IdStrumienia);
               return; }

            switch (status.Naglowek[0])
            {
                case Protokol.KoniecPolaczenia:
                    strumieniownia.ToPolaczenieNieDziala(status.IdStrumienia);
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.CzytajZawartosc(polaczenie.Strumien,status.IdStrumienia, 
                        polaczenie.IdUzytkownika, TypWiadomosci.Zwykla, dlugoscWiadomosci);
                    break;
                case Protokol.DajOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.IdStrumienia);
                    WyslijOpis(polaczenie.IdUzytkownika, ustawienia.Opis);
                    break;
                case Protokol.WezOpis: // my prosimy o opis
                    wiadomosciownia.CzytajZawartosc(polaczenie.Strumien, status.IdStrumienia, 
                        polaczenie.IdUzytkownika, TypWiadomosci.Opis, dlugoscWiadomosci);
                    break;
                case Protokol.WezPlik:
                    break;
                case Protokol.DajPlik:
                    break;
                default: // blad, rozlacz
                    strumieniownia.Rozlacz(status.IdStrumienia);
                    break;
            }
        }

        // strumien jest gotowy do czytania z niego
        void strumieniownia_GotowyDoOdbioru(string idStrumienia)
        { czekajNaZapytanie(idStrumienia); }

        // stworz komunikat
        byte[] stworzKomunikat(byte rodzaj, string wiadomosc)
        {
            var dlugoscZawartosci = Encoding.UTF8.GetByteCount(wiadomosc);
            var bajtyZawartosc = Encoding.UTF8.GetBytes(wiadomosc);
            var bajty = new byte[dlugoscZawartosci + DlugoscNaglowka];
            var dlugoscZawartosciNaglowek = BitConverter.GetBytes(dlugoscZawartosci);
            if (BitConverter.IsLittleEndian) { dlugoscZawartosciNaglowek.Reverse(); }
            bajty[0] = rodzaj;
            Array.Copy(dlugoscZawartosciNaglowek, 0, bajty, 1, dlugoscZawartosciNaglowek.Length);
            Array.Copy(bajtyZawartosc, 0, bajty, DlugoscNaglowka, bajtyZawartosc.Length);
            return bajty;
        }    

        // klasa uzywana do operacji asynchronicznej
        class StatusObsluzZapytanie
        {
            public byte[] Naglowek;
            public String IdStrumienia;
        }
    }
}
