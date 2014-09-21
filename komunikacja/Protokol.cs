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
        const int DlugoscNaglowka = 5; // 1 bajt na rodzaj komunikatu, 4 na dlugosc

        Dictionary<string, PolaczenieZasadnicze> polaczeniaZasadnicze = new Dictionary<string, PolaczenieZasadnicze>();
        Dictionary<string, PolaczeniePlikowe> polaczeniaPlikowe = new Dictionary<string, PolaczeniePlikowe>();

        public Protokol(Mapownik mapownik, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(new CzytanieSkonczone(czekajNaZapytanie)); ;
            

            foreach (var i in mapownik.WszystkieId) { wiadomosciownia.DodajUzytkownika(i); }

            this.ustawienia = ustawienia;
            this.mapownik = mapownik;

            centrala =  ustawienia.SSLWlaczone ? new CentralaSSL(ustawienia.Certyfikat): new Centrala();
            
            centrala.OtwartoPolaczenie += centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;

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
            var guid = centrala.Polacz(mapownik[id]);
            if (guid == null) { return; }
            if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == id))
            { polaczeniaZasadnicze.Add(guid, new PolaczenieZasadnicze() { IdUzytkownika = id }); }
            else
            { polaczeniaPlikowe.Add(guid, new PolaczeniePlikowe() { IdUzytkownika = id });  }
        }

        public void Rozlacz(string id) 
        {
            polaczeniaZasadnicze.Where(k => k.Value.IdUzytkownika == id).ToList().ForEach(
                k => centrala.Rozlacz(k.Key));
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
        { wiadomosciownia.WyslijWiadomosc(strumienZasadniczy(idRozmowcy) ,idRozmowcy, stworzKomunikat(rodzaj, wiadomosc)); }

        public void WyslijPlik(String idRozmowcy, String sciezka)
        { plikownia.Wyslij(idRozmowcy, sciezka); }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="idNadawcy">Identyfikator nadawcy</param>
        void czekajNaZapytanie(string guid)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
                        
            var kanal = polaczeniaZasadnicze[guid];
            var wynik = new StatusObsluzZapytanie() { guidStrumienia = guid, Naglowek = new byte[DlugoscNaglowka] };
            
            kanal.Strumien.BeginRead(wynik.Naglowek, 0, DlugoscNaglowka, obsluzZapytanie, wynik);
        }

        void centrala_ZamknietoPolaczenie(string guid)
        {
            if (polaczeniaZasadnicze.ContainsKey(guid)) {
                var kanal = polaczeniaZasadnicze[guid];
                polaczeniaZasadnicze.Remove(guid);
                polaczeniaPlikowe.Where(p => p.Value.IdUzytkownika == kanal.IdUzytkownika).ToList().
                    ForEach(p => centrala.Rozlacz(p.Key));
                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(kanal.IdUzytkownika); }
            
            }
            if (polaczeniaPlikowe.ContainsKey(guid)) { polaczeniaPlikowe.Remove(guid); }
        }

        void centrala_OtwartoPolaczenie(string guid, Stream strumien, IPAddress ip)
        {
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();
            
            Trace.TraceInformation("otwarto polaczenie");

            if (polaczeniaZasadnicze.ContainsKey(guid)) 
            {
                var kanal = polaczeniaZasadnicze[guid];
                kanal.Strumien = strumien;
                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
                
            }
            else if (polaczeniaPlikowe.ContainsKey(guid))
            {
                var kanal = polaczeniaPlikowe[guid];
                kanal.Strumien = strumien;
            }
            else if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == idUzytkownika))
            {
                polaczeniaZasadnicze.Add(guid, new PolaczenieZasadnicze() { IdUzytkownika = idUzytkownika, Strumien = strumien });

                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
            }
            else
            {
                polaczeniaPlikowe.Add(guid, new PolaczeniePlikowe() { IdUzytkownika = guid, Strumien = strumien });
            }
            czekajNaZapytanie(guid);
        }

        Stream strumienZasadniczy(string idUzytkownika) {
            return polaczeniaZasadnicze.Values.Where(p => p.IdUzytkownika == idUzytkownika).
                Select(p => p.Strumien).SingleOrDefault();
        }

        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.Naglowek[0].ToString());
            int dlugoscWiadomosci = BitConverter.ToInt32(status.Naglowek, 1);
            if (!polaczeniaZasadnicze.ContainsKey(status.guidStrumienia)) { return; }
            var kanal = polaczeniaZasadnicze[status.guidStrumienia];

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

        class PolaczenieZasadnicze 
        {
            public Stream Strumien { get; set; }
            public string IdUzytkownika { get; set; }
        }

        class PolaczeniePlikowe
        {
            public Stream Strumien { get; set; }
            public string IdUzytkownika { get; set; }
            public string Plik { get; set; }
        }

        class StatusObsluzZapytanie
        {
            public byte[] Naglowek;
            public String guidStrumienia;
        }
    }
}
