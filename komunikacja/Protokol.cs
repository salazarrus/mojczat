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

    class Protokol
    {
        public const byte KoniecPolaczenia = 0;
        public const byte ZwyklaWiadomosc = 1;
        public const byte DajOpis = 2;
        public const byte WezOpis = 3;
        public const byte WezPlik = 4;
        public const byte DajPlik = 5;

        Wiadomosciownia wiadomosciownia;
        Plikownia plikownia;
        Centrala centrala;
        Ustawienia ustawienia;
        Mapownik mapownik;
        Strumieniownia strumieniownia;

        const int DlugoscNaglowka = 5; // 1 bajt na rodzaj komunikatu, 4 na dlugosc


        public Protokol(Mapownik mapownik, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(new CzytanieSkonczone(czekajNaZapytanie)); ;
            

            foreach (var i in mapownik.WszystkieId) { wiadomosciownia.DodajUzytkownika(i); }

            this.ustawienia = ustawienia;
            this.mapownik = mapownik;

            centrala =  ustawienia.SSLWlaczone ? new CentralaSSL(ustawienia.Certyfikat): new Centrala();
            
            centrala.OtwartoPolaczenie += centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;

            strumieniownia = new Strumieniownia(mapownik, centrala);
            this.plikownia = new Plikownia();
        }

        public event NowaWiadomosc NowaWiadomosc {
            add { wiadomosciownia.NowaWiadomosc += value; }
            remove { wiadomosciownia.NowaWiadomosc -= value; }
        }

        public event OtwartoPolaczenieZasadnicze OtwartoPolaczenieZasadnicze;

        public event ZamknietoPolaczenieZasadnicze ZamknietoPolaczenieZasadnicze;

        public void Polacz(string id)
        {
            strumieniownia.Polacz(id);
        }

        public void Rozlacz(string id) 
        {
            strumieniownia.RozlaczUzytkownika(id);
        }

        public void Start()
        { centrala.Start(); }

        public void Stop()
        {
            centrala.OtwartoPolaczenie -= centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie -= centrala_ZamknietoPolaczenie;
            centrala.Stop();
        }

        public void DodajUzytkownika(string id)
        { wiadomosciownia.DodajUzytkownika(id); }

        public void UsunUzytkownika(string idUzytkownika)
        { wiadomosciownia.UsunUzytkownika(idUzytkownika); }

        public void WyslijWiadomosc(String idRozmowcy, byte rodzaj, String wiadomosc)
        { wiadomosciownia.WyslijWiadomosc( strumieniownia.StrumienZasadniczy(idRozmowcy) ,idRozmowcy, stworzKomunikat(rodzaj, wiadomosc)); }

        public void WyslijPlik(String idRozmowcy, String sciezka)
        { plikownia.Wyslij(idRozmowcy, sciezka); }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="idNadawcy">Identyfikator nadawcy</param>
        void czekajNaZapytanie(string guid)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");

            var polaczenie = strumieniownia.DajPolaczenieZasadnicze(guid); 
            var wynik = new StatusObsluzZapytanie() { guidStrumienia = guid, Naglowek = new byte[DlugoscNaglowka] };
            
            polaczenie.Strumien.BeginRead(wynik.Naglowek, 0, DlugoscNaglowka, obsluzZapytanie, wynik);
        }

     
        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.Naglowek[0].ToString());
            int dlugoscWiadomosci = BitConverter.ToInt32(status.Naglowek, 1);
            if (!strumieniownia.CzyZnasz(status.guidStrumienia)) { return; }
            var kanal = strumieniownia.DajPolaczenieZasadnicze(status.guidStrumienia);

            try { kanal.Strumien.EndRead(wynik); }
            catch // zostalismy rozlaczeni
            {  centrala.ToNieDziala(status.guidStrumienia);
               return; }

            switch (status.Naglowek[0])
            {
                case Protokol.KoniecPolaczenia:
                    centrala.ToNieDziala(status.guidStrumienia);
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.CzytajZawartosc(kanal.Strumien,status.guidStrumienia, 
                        kanal.IdUzytkownika, TypWiadomosci.Zwykla, dlugoscWiadomosci);
                    break;
                case Protokol.DajOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.guidStrumienia);
                    WyslijWiadomosc(kanal.IdUzytkownika, Protokol.WezOpis, ustawienia.Opis);
                    break;
                case Protokol.WezOpis: // my prosimy o opis
                    wiadomosciownia.CzytajZawartosc(kanal.Strumien, status.guidStrumienia, 
                        kanal.IdUzytkownika, TypWiadomosci.Opis, dlugoscWiadomosci);
                    break;
                case Protokol.WezPlik:
                    break;
                case Protokol.DajPlik:
                    break;
                default: // blad, rozlacz
                    centrala.Rozlacz(status.guidStrumienia);
                    break;
            }
        }

        void centrala_OtwartoPolaczenie(string guid, Stream strumien, IPAddress ip)
        {
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();

            Trace.TraceInformation("otwarto polaczenie");
            var polaczenie = strumieniownia.DajPolaczenieZasadnicze(guid);
            
            if (polaczenie is PolaczenieZasadnicze)
            {
                polaczenie.Strumien = strumien;
                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }

            }
            else if (polaczenie is PolaczeniePlikowe)
            {
                polaczenie.Strumien = strumien;
            }
            else if (!strumieniownia.IstniejePolaczenieZasadnicze(idUzytkownika))
            {
                 strumieniownia.DodajPolaczenie(guid, new PolaczenieZasadnicze() 
                    { IdUzytkownika = idUzytkownika, Strumien = strumien });

                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
            }
            else
            {
                strumieniownia.DodajPolaczenie(guid, new PolaczeniePlikowe() 
                    { IdUzytkownika = guid, Strumien = strumien });
            }
            czekajNaZapytanie(guid);
        }



        void centrala_ZamknietoPolaczenie(string guid)
        {
            if ( strumieniownia.DajPolaczenieZasadnicze(guid) != null)
            {
                var kanal = strumieniownia.DajPolaczenieZasadnicze(guid);
                strumieniownia.Usun(guid);
                strumieniownia.RozlaczPolaczeniaPlikowe(kanal.IdUzytkownika);

                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(kanal.IdUzytkownika); }

            }
            if (strumieniownia.DajPolaczeniePlikowe(guid) != null) { strumieniownia.Usun(guid); }
        }


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

        class StatusObsluzZapytanie
        {
            public byte[] Naglowek;
            public String guidStrumienia;
        }
    }
}
