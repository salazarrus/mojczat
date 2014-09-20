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
        public const byte DajMiSwojOpis = 2;
        public const byte OtoMojOpis = 3;

        Wiadomosciownia wiadomosciownia;
        Plikownia plikownia;
        Centrala centrala;
        Ustawienia ustawienia;
        Mapownik mapownik;
        const int DlugoscNaglowka = 5; // 1 bajt na rodzaj komunikatu, 4 na dlugosc

        Dictionary<string, Kanal> polaczenia = new Dictionary<string, Kanal>();

        public Protokol(Mapownik mapownik, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(centrala,
                new CzytanieSkonczone(czekajNaZapytanie)); ;
            //this.plikownia = new Plikownia();

            foreach (var i in mapownik.WszystkieId) { wiadomosciownia.DodajUzytkownika(i); }

            this.ustawienia = ustawienia;
            this.mapownik = mapownik;

            centrala =  ustawienia.SSLWlaczone ? new CentralaSSL(ustawienia.Certyfikat): new Centrala();
            
        }

        public event NowaWiadomosc NowaWiadomosc {
            add { wiadomosciownia.NowaWiadomosc += value; }
            remove { wiadomosciownia.NowaWiadomosc -= value; }
        }

        public event OtwartoPolaczenieZasadnicze OtwartoPolaczenieZasadnicze;

        public event ZamknietoPolaczenieZasadnicze ZamknietoPolaczenieZasadnicze;

        public void Polacz(string id)
        {
            centrala.Polacz(mapownik[id]);
        }

        public void Rozlacz(string id) 
        {
            polaczenia.Where(k => k.Value.IdUzytkownika == id).ToList().ForEach(
                k => centrala.Rozlacz(k.Key));
        }

        public void Start()
        {
            centrala.OtwartoPolaczenie += centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;
            centrala.Start();
        }
        
        public void Stop() 
        {
            //centrala.OtwartoPolaczenie -= centrala_OtwartoPolaczenie;
            //centrala.ZamknietoPolaczenie -= centrala_ZamknietoPolaczenie;
            centrala.Stop();
        }

        public void DodajUzytkownika(string id)
        { wiadomosciownia.DodajUzytkownika(id); }

        public void UsunUzytkownika(string idUzytkownika)
        { wiadomosciownia.UsunUzytkownika(idUzytkownika); }

        public void WyslijWiadomosc(String idRozmowcy, byte rodzaj, String wiadomosc)
        { wiadomosciownia.WyslijWiadomosc(strumienZasadniczy(idRozmowcy) ,idRozmowcy, rodzaj, wiadomosc); }

        public void WyslijPlik(String idRozmowcy, String sciezka)
        { plikownia.Wyslij(idRozmowcy, sciezka); }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="idNadawcy">Identyfikator nadawcy</param>
        public void czekajNaZapytanie(string guid)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
                        
            var kanal = polaczenia[guid];
            var wynik = new StatusObsluzZapytanie() { guidStrumienia = guid, Naglowek = new byte[DlugoscNaglowka] };
            
            kanal.strumien.BeginRead(wynik.Naglowek, 0, DlugoscNaglowka, obsluzZapytanie, wynik);
        }

        void centrala_ZamknietoPolaczenie(string guid)
        {
            if (!polaczenia.ContainsKey(guid)) { return; }
            var kanal = polaczenia[guid];
            polaczenia.Remove(guid);
            if (kanal.Typ == KanalTyp.ZASADNICZY)
            {
                // zamknij inne polaczenia od tegu uzytkownika
                polaczenia.Where(p => p.Value.IdUzytkownika == kanal.IdUzytkownika).ToList().
                    ForEach(p => centrala.Rozlacz(p.Key));
                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(kanal.IdUzytkownika); }
            }
        }

        void centrala_OtwartoPolaczenie(string guid, Stream strumien, IPAddress ip)
        {
            var mamyZasadnicze = polaczenia.Values.Any(p => p.IdUzytkownika == mapownik[ip]
                && p.Typ == KanalTyp.ZASADNICZY);
            Kanal kanal = new Kanal()
            {
                IdUzytkownika = mapownik[ip],
                strumien = strumien,
                Typ = mamyZasadnicze ? KanalTyp.DODATKOWY : KanalTyp.ZASADNICZY
            };

            polaczenia.Add(guid, kanal);
            czekajNaZapytanie(guid);
            
            if (!mamyZasadnicze && OtwartoPolaczenieZasadnicze != null)
            { OtwartoPolaczenieZasadnicze(mapownik[ip]); } // TODO co z nieznajomym          
        }

        Stream strumienZasadniczy(string idUzytkownika) {
            return polaczenia.Values.Where(p => p.IdUzytkownika == idUzytkownika && p.Typ == KanalTyp.ZASADNICZY).
                Select(p => p.strumien).SingleOrDefault();
        }

        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.Naglowek[0].ToString());
            int dlugoscWiadomosci = BitConverter.ToInt32(status.Naglowek, 1);
            var kanal = polaczenia[status.guidStrumienia];

            try { kanal.strumien.EndRead(wynik); }
            catch (Exception ex)
            {   // zostalismy rozlaczeni
                centrala.ToNieDziala(status.guidStrumienia);
                return;
            }

            switch (status.Naglowek[0])
            {
                case Protokol.KoniecPolaczenia:
                    centrala.ToNieDziala(status.guidStrumienia);
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.CzytajZawartosc(kanal.strumien,status.guidStrumienia , kanal.IdUzytkownika, TypWiadomosci.Zwykla, dlugoscWiadomosci);
                    break;
                case Protokol.DajMiSwojOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.guidStrumienia);
                    wiadomosciownia.WyslijWiadomosc(kanal.strumien, kanal.IdUzytkownika, Protokol.OtoMojOpis, ustawienia.Opis);
                    break;
                case Protokol.OtoMojOpis: // my prosimy o opis
                    wiadomosciownia.CzytajZawartosc(kanal.strumien, status.guidStrumienia, kanal.IdUzytkownika, TypWiadomosci.Opis, dlugoscWiadomosci);
                    break;
                default: // blad, czekaj na kolejne zapytanie
                    czekajNaZapytanie(status.guidStrumienia); // TODO zamknij to polaczenie
                    break;
            }
        }

        class Kanal 
        {
            public Stream strumien { get; set; }
            public KanalTyp Typ { get; set; }
            public string IdUzytkownika { get; set; }
        }

        enum KanalTyp
        {
            ZASADNICZY,
            DODATKOWY
        }

        class StatusObsluzZapytanie
        {
            public byte[] Naglowek;
            public String guidStrumienia;
        }
    }
}
