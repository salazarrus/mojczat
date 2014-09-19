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
        Buforownia buforownia;
        CzytanieSkonczone czytanieSkonczone;

        public Wiadomosciownia(Buforownia buforownia, Centrala centrala, CzytanieSkonczone czytanieSkonczone)
        {
            this.centrala = centrala;
            this.buforownia = buforownia;
            this.czytanieSkonczone = czytanieSkonczone;
        }

        public event NowaWiadomosc NowaWiadomosc;

        public void czytajWiadomosc(string id, TypWiadomosci rodzaj)
        {
            centrala[id].BeginRead(buforownia[id], 0,
                Buforownia.RozmiarBufora, new AsyncCallback(obsluzWiadomosc),
                new CzytajWiadomoscStatus() { id = id, rodzaj = rodzaj });
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
        }


        byte[] stworzKomunikat(byte rodzaj, string wiadomosc)
        {
            wiadomosc = Encoding.UTF8.GetString(new byte[] { rodzaj }) + wiadomosc;
            return Encoding.UTF8.GetBytes(wiadomosc);
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
        
        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void obsluzWiadomosc(IAsyncResult wynik)
        {
            // od kogo przyszla wiadomosc
            var status = (CzytajWiadomoscStatus)wynik.AsyncState;
            // zakoncz operacje asynchroniczna
            var strumien = centrala[status.id];
            try
            {
                strumien.EndRead(wynik);
            }
            catch (Exception ex) {
                Trace.TraceInformation(ex.ToString());
            }
            int index = Array.FindIndex(buforownia[status.id], x => x == 0);
            // dekodujemy wiadomosc
            // usuwamy \0 z konca lancucha
            string wiadomosc = index >= 0 ?
                Encoding.UTF8.GetString(buforownia[status.id], 0, index) :
                Encoding.UTF8.GetString(buforownia[status.id]);

            // czyscimy bufor
            Array.Clear(buforownia[status.id], 0, Buforownia.RozmiarBufora);
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if (NowaWiadomosc != null)
            {
                // informujemy zainteresowanych
                NowaWiadomosc(status.id, status.rodzaj, wiadomosc);
            }

            // rozpocznij nowa operacje asynchroniczna - czekaj na nowa wiadomosc
            czytanieSkonczone(status.id);
        }

        class CzytajWiadomoscStatus
        {
            public String id { get; set; }
            public TypWiadomosci rodzaj { get; set; }
        }

    }

    public enum TypWiadomosci
    {
        Zwykla,
        Opis
    }
}
