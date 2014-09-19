using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    class Protokol
    {
        public const byte KoniecPolaczenia = 0;
        public const byte ZwyklaWiadomosc = 1;
        public const byte DajMiSwojOpis = 2;
        public const byte OtoMojOpis = 3;

        Wiadomosciownia wiadomosciownia;
        Centrala centrala;
        Ustawienia ustawienia;
        Mapownik mapownik;

        public Protokol(Centrala centrala, Buforownia buforownia , Mapownik mapownik, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(buforownia, centrala,
                new CzytanieSkonczone(czekajNaZapytanie)); ;

            foreach (var i in mapownik.WszystkieId) { wiadomosciownia.DodajUzytkownika(i); }

            this.ustawienia = ustawienia;
            this.centrala = centrala;
            this.mapownik = mapownik;
        }

        public event NowaWiadomosc NowaWiadomosc {
            add {
                wiadomosciownia.NowaWiadomosc += value;
            }
            remove {
                wiadomosciownia.NowaWiadomosc -= value;
            }
        }

        public void DodajUzytkownika(string id)
        {
            wiadomosciownia.DodajUzytkownika(id);
        }

        public void UsunUzytkownika(string idUzytkownika){
            wiadomosciownia.UsunUzytkownika(idUzytkownika);
        }

        public void WyslijWiadomosc(String idRozmowcy, byte rodzaj, String wiadomosc)
        {
            wiadomosciownia.WyslijWiadomosc(idRozmowcy, rodzaj, wiadomosc);
        }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="idNadawcy">Identyfikator nadawcy</param>
        public void czekajNaZapytanie(string idNadawcy)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
            var wynik = new StatusObsluzZapytanie() { idNadawcy = idNadawcy, rodzaj = new byte[1] };
            if (centrala[mapownik[idNadawcy]] == null)
            {
                Trace.TraceInformation("[czekajNaZapytanie] brak polaczenia");
                return;
            }
            centrala[mapownik[idNadawcy]].BeginRead(wynik.rodzaj, 0, 1, obsluzZapytanie, wynik);
        }

        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            if (centrala[mapownik[status.idNadawcy]] == null) { return; }
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.rodzaj[0].ToString());

            try { centrala[mapownik[status.idNadawcy]].EndRead(wynik); }
            catch (Exception ex)
            {   // zostalismy rozlaczeni
                centrala.ToNieDziala(status.idNadawcy);
                return;
            }

            switch (status.rodzaj[0])
            {
                case Protokol.KoniecPolaczenia:
                    centrala.ToNieDziala(status.idNadawcy);
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, TypWiadomosci.Zwykla);
                    break;
                case Protokol.DajMiSwojOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.idNadawcy);
                    wiadomosciownia.WyslijWiadomosc(status.idNadawcy, Protokol.OtoMojOpis, ustawienia.Opis);
                    break;
                case Protokol.OtoMojOpis: // my prosimy o opis
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, TypWiadomosci.Opis);
                    break;
                default: // blad, czekaj na kolejne zapytanie
                    czekajNaZapytanie(status.idNadawcy);
                    break;
            }
        }

        class StatusObsluzZapytanie
        {
            public byte[] rodzaj;
            public String idNadawcy;
        }
    }
}
