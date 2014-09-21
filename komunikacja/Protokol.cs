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
    public delegate void NawiazalismyPolaczeniePlikowe(string idPolaczenia);
    
    public delegate void CzytanieSkonczone(string idUzytkownika);
    
    /// <summary>
    /// Obiekt odpowiedzialny za koordynacje przesylania i odbierania komunikatow
    /// </summary>
    class Protokol
    {
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
            strumieniownia.NawiazalismyPolaczeniePlikowe += strumieniownia_NawiazalismyPolaczeniePlikowe;
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

        public event PlikZaoferowano PlikZaoferowano
        {
            add { plikownia.PlikZaoferowano += value; }
            remove { plikownia.PlikZaoferowano -= value; }
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
        public void Polacz(string idUzytkownika) { strumieniownia.NawiazPolaczenieZasadnicze(idUzytkownika); }

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
        { wyslijWiadomosc(idUzytkownika, Komunikat.ZwyklaWiadomosc, wiadomosc); }

        /// <summary>
        /// Wyslij swoj opis
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="opis">tekst opisu</param>
        public void WyslijOpis(String idUzytkownika, String opis)
        { wyslijWiadomosc(idUzytkownika, Komunikat.WezOpis, opis); }

        /// <summary>
        /// Popros innego uzytkownika o jego opis
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void PoprosOpis(String idUzytkownika)
        { wyslijWiadomosc(idUzytkownika, Komunikat.DajOpis, ""); }

        /// <summary>
        /// Wyslij plik to innego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="sciezka">sciezka do pliku</param>
        public void WyslijPlik(String idUzytkownika, String sciezka)
        {
            strumieniownia.NawiazPolaczeniePlikowe(sciezka, idUzytkownika); 
        }

        // wyslij wiadomosc tekstowa
        void wyslijWiadomosc(String idRozmowcy, byte rodzaj, String wiadomosc)
        {
            wiadomosciownia.WyslijWiadomosc(strumieniownia.DajStrumienZasadniczy(idRozmowcy)
              , idRozmowcy, Komunikat.Generuj(rodzaj, wiadomosc));
        }

        void strumieniownia_NawiazalismyPolaczeniePlikowe(string idPolaczenia)
        {
            var polaczenie = (PolaczeniePlikowe)strumieniownia.DajPolaczenie(idPolaczenia);
            plikownia.OferujPlik(polaczenie.IdUzytkownika, polaczenie.Plik, polaczenie.Strumien);
            czekajNaZapytanie(idPolaczenia);
        }   

        // Czekaj (pasywnie) na zapytania i wiadomosci
        void czekajNaZapytanie(string idPolaczenia)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
            var polaczenie = strumieniownia.DajPolaczenie(idPolaczenia); 
            var wynik = new StatusObsluzZapytanie() { IdStrumienia = idPolaczenia, Naglowek = new byte[DlugoscNaglowka] };
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
                case Komunikat.KoniecPolaczenia:
                    strumieniownia.ToPolaczenieNieDziala(status.IdStrumienia);
                    return;
                case Komunikat.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.CzytajZawartosc(polaczenie.Strumien,status.IdStrumienia, 
                        polaczenie.IdUzytkownika, TypWiadomosci.Zwykla, dlugoscWiadomosci);
                    break;
                case Komunikat.DajOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.IdStrumienia);
                    WyslijOpis(polaczenie.IdUzytkownika, ustawienia.Opis);
                    break;
                case Komunikat.WezOpis: // my prosimy o opis
                    wiadomosciownia.CzytajZawartosc(polaczenie.Strumien, status.IdStrumienia, 
                        polaczenie.IdUzytkownika, TypWiadomosci.Opis, dlugoscWiadomosci);
                    break;
                case Komunikat.WezPlik:
                    Trace.TraceInformation("zaoferowano nam plik");
                    plikownia.WczytajNazwe(polaczenie.Strumien, polaczenie.IdUzytkownika, dlugoscWiadomosci);
                    //strumieniownia.Rozlacz(status.IdStrumienia);
                    break;
                case Komunikat.DajPlik:
                    break;
                default: // blad, rozlacz
                    strumieniownia.Rozlacz(status.IdStrumienia);
                    break;
            }
        }

        // strumien jest gotowy do czytania z niego
        void strumieniownia_GotowyDoOdbioru(string idStrumienia)
        { czekajNaZapytanie(idStrumienia); }

       

        // klasa uzywana do operacji asynchronicznej
        class StatusObsluzZapytanie
        {
            public byte[] Naglowek;
            public String IdStrumienia;
        }
    }
}
