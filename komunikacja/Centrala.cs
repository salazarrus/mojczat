using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MojCzat.komunikacja
{
    delegate void OtwartoPolaczenie(string idStrumienia, Kierunek kierunek, Stream strumien, IPAddress ip);
    delegate void ZamknietoPoloczenie(string idStrumienia);

    /// <summary>
    /// Obiekt odpowiedzialny za otwieranie i zamykanie polaczen z innymi uzytkownikami
    /// </summary>
    class Centrala
    {
        // Polaczenia TCP ktore zostaly otwarte. Klucz to indentyfikator polaczenia.
        Dictionary<string, TcpClient> polaczenia = new Dictionary<string, TcpClient>();

        // Strumienie ktore zostaly otwarte. Klucz to indentyfikator polaczenia.
        Dictionary<string, Stream> strumienie = new Dictionary<string, Stream>();

        // obiekt nasluchujacy nadchodzacych polaczen
        TcpListener serwer;

        const int POLACZENIE_TIMEOUT = 1000;

        protected virtual int Port { get { return 5080; } }

        /// <summary>
        /// Otworzylismy nowe polaczenie
        /// </summary>
        public event OtwartoPolaczenie OtwartoPolaczenie;

        /// <summary>
        /// Zamknelismy polaczenie
        /// </summary>
        public event ZamknietoPoloczenie ZamknietoPolaczenie;

        /// <summary>
        /// Oczekuj nadchodzacych polaczen 
        /// </summary>
        public void Start()
        {
            try
            {
                serwer = new TcpListener(IPAddress.Any, Port); // stworz serwer
                serwer.Start(); //uruchom serwer

                while (true) // zapetlamy
                {
                    // czekaj na przychodzace polaczenia
                    TcpClient polaczenie = serwer.AcceptTcpClient();
                    var strumien = dajStrumienJakoSerwer(polaczenie);
                    zachowajNowePolaczenie(polaczenie, Kierunek.DO_NAS, Guid.NewGuid().ToString(), strumien);
                }
            }
            catch { } // program zostal zamkniety
            finally { Stop(); }
        }

        /// <summary>
        /// zatrzymaj nasluch 
        /// </summary>
        public void Stop()
        {
            if (serwer != null)
            {
                serwer.Stop();
                RozlaczWszystkich();
            }
        }

        /// <summary>
        /// Raportowanie o niedzialajacym polaczeniu
        /// </summary>
        /// <param name="idPolaczenia"></param>
        public void ToNieDziala(string idPolaczenia)
        { Rozlacz(idPolaczenia); }

        /// <summary>
        /// Polacz z wybranym adresem IP
        /// </summary>
        /// <param name="ip">adres IP</param>
        /// <returns>identyfikator, ktory otrzyma polaczenie</returns>
        public void Polacz(string idPolaczenia, IPAddress ip)
        {
            // tworzymy nowe polaczenie 
            var klient = new TcpClient();
            var wynik = klient.BeginConnect(ip, Port, new AsyncCallback(nawiazPolaczenieWynik),
                new NawiazPolaczenieStatus() { IdStrumienia = idPolaczenia, Polaczenie = klient });

            if (!wynik.AsyncWaitHandle.WaitOne(POLACZENIE_TIMEOUT, true))
            { Rozlacz(idPolaczenia); }
        }



        /// <summary>
        /// Zamknij dane polaczenie
        /// </summary>
        /// <param name="idPolaczenia">Identyfikator polaczenia</param>
        public void Rozlacz(string idPolaczenia)
        {
            if (polaczenia.ContainsKey(idPolaczenia))
            {
                try { polaczenia[idPolaczenia].Close(); }
                catch { }
                polaczenia.Remove(idPolaczenia);
            }

            if (strumienie.ContainsKey(idPolaczenia))
            {
                try { strumienie[idPolaczenia].Close(); }
                catch { }
                strumienie.Remove(idPolaczenia);
            }
            if (ZamknietoPolaczenie != null) { ZamknietoPolaczenie(idPolaczenia); }
        }

        /// <summary>
        /// Rozlacz wszystkie polaczenia
        /// </summary>
        public void RozlaczWszystkich()
        { polaczenia.Keys.ToList().ForEach(i => Rozlacz(i)); }

        protected virtual Stream dajStrumienJakoKlient(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

        protected virtual Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

        // udalo sie badz nie nawiac polaczenie
        void nawiazPolaczenieWynik(IAsyncResult wynik)
        {
            var status = (NawiazPolaczenieStatus)wynik.AsyncState;
            try
            {
                status.Polaczenie.EndConnect(wynik);

                if (!status.Polaczenie.Connected)
                {
                    status.Polaczenie.Close();
                    return;
                }

                var strumien = dajStrumienJakoKlient(status.Polaczenie);
                zachowajNowePolaczenie(status.Polaczenie, Kierunek.OD_NAS, status.IdStrumienia, strumien);
            }
            catch
            { if (ZamknietoPolaczenie != null) { ZamknietoPolaczenie(status.IdStrumienia); } }
        }

        // zachowujemy polaczenie na pozniej
        void zachowajNowePolaczenie(TcpClient polaczenie, Kierunek kierunek, string idStrumienia, Stream strumien)
        {
            polaczenia.Add(idStrumienia, polaczenie);
            strumienie.Add(idStrumienia, strumien);
            var ip = ((IPEndPoint)polaczenie.Client.RemoteEndPoint).Address;

            if (OtwartoPolaczenie != null) { OtwartoPolaczenie(idStrumienia, kierunek, strumien, ip); }
        }

        // obiekt uzywany do operacji asynchronicznej
        class NawiazPolaczenieStatus
        {
            public TcpClient Polaczenie { get; set; }
            public string IdStrumienia { get; set; }
        }
    }
}
