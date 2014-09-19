﻿using MojCzat.model;
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

        Dictionary<string, IPAddress> ID_IP;

 
        Dictionary<IPAddress, string> IP_ID;
        Wiadomosciownia wiadomosciownia;
        Centrala centrala;
        Ustawienia ustawienia;

        public Protokol(Centrala centrala, Buforownia buforownia , Dictionary<string, IPAddress> ID_IP,
            Dictionary<IPAddress, string> IP_ID, Ustawienia ustawienia) {
            this.wiadomosciownia = new Wiadomosciownia(buforownia, centrala,
                new CzytanieSkonczone(czekajNaZapytanie)); ;

            foreach (var i in ID_IP) { wiadomosciownia.DodajUzytkownika(i.Key); }

            this.ustawienia = ustawienia;
            this.centrala = centrala;
            this.ID_IP = ID_IP;
            this.IP_ID = IP_ID;
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
            if (centrala[dajIp(idNadawcy)] == null)
            {
                Trace.TraceInformation("[czekajNaZapytanie] brak polaczenia");
                return;
            }
            centrala[dajIp(idNadawcy)].BeginRead(wynik.rodzaj, 0, 1, obsluzZapytanie, wynik);
        }

        void obsluzZapytanie(IAsyncResult wynik)
        {
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            if (centrala[dajIp(status.idNadawcy)] == null) { return; }
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.rodzaj[0].ToString());

            try { centrala[dajIp(status.idNadawcy)].EndRead(wynik); }
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
        IPAddress dajIp(string idUzytkownika)
        {
            return ID_IP[idUzytkownika];
        }

        class StatusObsluzZapytanie
        {
            public byte[] rodzaj;
            public String idNadawcy;
        }

    }
}
