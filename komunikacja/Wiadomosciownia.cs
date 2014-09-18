using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    class Wiadomosciownia
    {
        public event NowaWiadomosc NowaWiadomosc;

        Dictionary<string, Queue<byte[]>> komunikatyWychodzace = new Dictionary<string, Queue<byte[]>>();
        Dictionary<string, Boolean> wysylanieWToku = new Dictionary<string, Boolean>();
        Dictionary<string, object> zamkiWysylania = new Dictionary<string, object>();

        Centrala centrala;
        Buforownia buforownia;
        done done;



        public Wiadomosciownia(Buforownia buforownia, Centrala centrala, done done) {
            this.centrala = centrala;
            this.buforownia = buforownia;
            this.done = done;
        }

        public void DodajUzytkownika(string id) {
            wysylanieWToku.Add(id, false);
            zamkiWysylania.Add(id, new object());
        }

        public void UsunUzytkownika(string idUzytkownika)
        {
            zamkiWysylania.Remove(idUzytkownika);
            wysylanieWToku.Remove(idUzytkownika);
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public bool WyslijWiadomosc(String idRozmowcy, byte rodzaj ,String wiadomosc)
        {
            try
            {
                if (string.IsNullOrEmpty(wiadomosc)) { return false; }
                Stream strumien = centrala[idRozmowcy];
                // tranformacja tekstu w bajty

                Byte[] bajty = stworzKomunikat(rodzaj, wiadomosc);
                // wysylanie bajtow polaczeniem TCP 
                dajKolejkeWiadomosci(idRozmowcy).Enqueue(bajty);
                wysylajZKolejki(idRozmowcy);
                return true;// TODO zmienic
            }
            catch { return false; }
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
                if (wysylanieWToku[idUzytkownika])
                {
                    return;
                }

                if (dajKolejkeWiadomosci(idUzytkownika).Any())
                {
                    doWyslania = dajKolejkeWiadomosci(idUzytkownika).Dequeue();
                    wysylanieWToku[idUzytkownika] = true;
                }
            }

            if (doWyslania != null)
            {
                centrala[idUzytkownika].BeginWrite
                          (doWyslania, 0, doWyslania.Length, new AsyncCallback(komunikatWyslany), idUzytkownika);
            }
        }

        void komunikatWyslany(IAsyncResult wynik)
        {
            var id = (string)wynik.AsyncState;
            centrala[id].EndWrite(wynik);

            lock (zamkiWysylania[id])
            {
                wysylanieWToku[id] = false;
            }
            wysylajZKolejki(id);
        }


        void wyslijOpis(string id, string opis)
        {
            dajKolejkeWiadomosci(id).Enqueue(stworzKomunikat(Protokol.OtoMojOpis, opis));
            wysylajZKolejki(id);
        }

        public void czytajWiadomosc(string id, Wiadomosc rodzaj)
        {
            centrala[id].BeginRead(buforownia[id], 0,
                Buforownia.RozmiarBufora, new AsyncCallback(obsluzWiadomosc),
                new CzytajWiadomoscStatus() { id = id, rodzaj = rodzaj });
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
            strumien.EndRead(wynik);

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
            done(status.id);
        }

        class CzytajWiadomoscStatus
        {
            public String id { get; set; }
            public Wiadomosc rodzaj { get; set; }
        }

    }
}
