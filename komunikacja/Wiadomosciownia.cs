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

        Centrala centrala;
        Buforownia buforownia = new Buforownia();
        CzytanieSkonczone czytanieSkonczone;
        const int DlugoscNaglowka = 5; 

        public Wiadomosciownia(Centrala centrala, CzytanieSkonczone czytanieSkonczone)
        {
            this.centrala = centrala;
            this.czytanieSkonczone = czytanieSkonczone;
        }

        public event NowaWiadomosc NowaWiadomosc;

        public void CzytajZawartosc(string id, TypWiadomosci rodzaj, int dlugoscWiadomosci)
        {
            if (dlugoscWiadomosci == 0) { czytanieSkonczone(id); }
            czytajZawartosc(id, rodzaj, dlugoscWiadomosci, 0);    
        }           
        
        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(String idRozmowcy, byte rodzaj ,String wiadomosc)
        {
            try
            {
                Trace.TraceInformation(String.Format("Probojemy wyslac wiadomosc rodzaj {0}: {1} ", rodzaj, wiadomosc));
                if (string.IsNullOrEmpty(wiadomosc) && rodzaj != Protokol.DajMiSwojOpis) { return;  }
                Stream strumien = centrala[idRozmowcy];
                // tranformacja tekstu w bajty
                Trace.TraceInformation(String.Format("Wysylamy wiadomosc rodzaj {0}: {1} ",rodzaj, wiadomosc));

                Byte[] bajty = stworzKomunikat(rodzaj, wiadomosc);
                // wysylanie bajtow polaczeniem TCP 
                dajKolejkeWiadomosci(idRozmowcy).Enqueue(bajty);
                wysylajZKolejki(idRozmowcy);
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

        Queue<byte[]> dajKolejkeWiadomosci(string id)
        {
            if (!komunikatyWychodzace.ContainsKey(id))
            {
                komunikatyWychodzace.Add(id, new Queue<byte[]>());
            }
            return komunikatyWychodzace[id];
        }
        
        void wysylajZKolejki(string idUzytkownika)
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
                centrala[idUzytkownika].BeginWrite(doWyslania, 0, 
                    doWyslania.Length, new AsyncCallback(komunikatWyslany), idUzytkownika);
            }
        }

        void komunikatWyslany(IAsyncResult wynik)
        {
            var idUzytkownika = (string)wynik.AsyncState;
            centrala[idUzytkownika].EndWrite(wynik);

            lock (zamkiWysylania[idUzytkownika])
            { wysylanieWToku[idUzytkownika] = false; }
            wysylajZKolejki(idUzytkownika);
        }


        void czytajZawartosc(string id, TypWiadomosci rodzaj, int dlugoscWiadomosci, int wczytano)
        {
            centrala[id].BeginRead(buforownia[id], wczytano,
                dlugoscWiadomosci, new AsyncCallback(zawartoscWczytana),
                new CzytajWiadomoscStatus() { IdNadawcy = id, Rodzaj = rodzaj, 
                    DlugoscWiadomosci = dlugoscWiadomosci, Wczytano = wczytano });
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
            var strumien = centrala[status.IdNadawcy];
            
            try
            {
                int bajtyWczytane = strumien.EndRead(wynik);
                if (status.DlugoscWiadomosci > status.Wczytano + bajtyWczytane)
                { 
                    czytajZawartosc(status.IdNadawcy, status.Rodzaj, 
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

            czytanieSkonczone(status.IdNadawcy);
        }

        class CzytajWiadomoscStatus
        {
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
