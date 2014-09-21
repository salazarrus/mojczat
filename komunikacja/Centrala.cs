﻿#define TRACE

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
    public delegate void OtwartoPolaczenie(string guid, Stream strumien, IPAddress ip);
    public delegate void ZamknietoPoloczenie(string guid);
    
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

        const int POLOCZENIE_TIMEOUT = 1000;

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
                int portMoj;
                int.TryParse(ConfigurationManager.AppSettings["portMoj"], out portMoj);
                serwer = new TcpListener(IPAddress.Any, portMoj); // stworz serwer
                serwer.Start(); //uruchom serwer

                while (true) // zapetlamy
                {
                    // czekaj na przychodzace polaczenia
                    TcpClient polaczenie = serwer.AcceptTcpClient();
                    var strumien = dajStrumienJakoSerwer(polaczenie);
                    zachowajNowePolaczenie(polaczenie, Guid.NewGuid().ToString(), strumien);
                }
            }
            catch (Exception ex) { Trace.TraceInformation("[Start]" + ex.ToString()); } // program zostal zamkniety
            finally { Stop(); }
        }

        /// <summary>
        /// zatrzymaj nasluch 
        /// </summary>
        public void Stop() 
        { 
            if (serwer != null) { 
                serwer.Stop();
                RozlaczWszystkich();
            } 
        }

        /// <summary>
        /// Raportowanie o niedzialajacym polaczeniu
        /// </summary>
        /// <param name="idPolaczenia"></param>
        public void ToNieDziala(string idPolaczenia) {
            Rozlacz(idPolaczenia);
        }
        
        /// <summary>
        /// Polacz z wybranym adresem IP
        /// </summary>
        /// <param name="ip">adres IP</param>
        /// <returns>identyfikator, ktory otrzyma polaczenie</returns>
        public string Polacz(IPAddress ip)
        {
            // tworzymy nowe polaczenie 
            Trace.TraceInformation("nawiazujemy polaczenie");
            var guid = Guid.NewGuid().ToString();
            var klient = new TcpClient();
            int portJego;
            int.TryParse(ConfigurationManager.AppSettings["portJego"], out portJego);
            var wynik = klient.BeginConnect(ip, portJego, new AsyncCallback(nawiazPolaczenieWynik),
                new NawiazPolaczenieStatus() { Guid = guid, Polaczenie = klient });
            
            if (!wynik.AsyncWaitHandle.WaitOne(POLOCZENIE_TIMEOUT, true)) 
            {
                Trace.TraceInformation("timeout nawiaz polaczenie");
                return null;
            }
            return guid;
        }

        /// <summary>
        /// Zamknij dane polaczenie
        /// </summary>
        /// <param name="idPolaczenia">Identyfikator polaczenia</param>
        public void Rozlacz(string idPolaczenia)
        {
            if (polaczenia.ContainsKey(idPolaczenia))
            {
                try { polaczenia[idPolaczenia].Close(); } catch { }
                polaczenia.Remove(idPolaczenia);
            }

            if (strumienie.ContainsKey(idPolaczenia))
            {
                try { strumienie[idPolaczenia].Close(); } catch { }
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
                {   status.Polaczenie.Close();
                    return; }

                var strumien = dajStrumienJakoKlient(status.Polaczenie);
                zachowajNowePolaczenie(status.Polaczenie, status.Guid, strumien);
            }
            catch
            { if (ZamknietoPolaczenie != null) { ZamknietoPolaczenie(status.Guid); } }
        }

        // zachowujemy polaczenie na pozniej
        void zachowajNowePolaczenie(TcpClient polaczenie, string guid ,Stream strumien)
        {
            Trace.TraceInformation("Zachowujemy polaczenie");

            polaczenia.Add(guid, polaczenie);
            strumienie.Add(guid, strumien);
            var ip = ((IPEndPoint)polaczenie.Client.RemoteEndPoint).Address;

            if (OtwartoPolaczenie != null) { OtwartoPolaczenie(guid, strumien, ip); }
        }

        // obiekt uzywany do operacji asynchronicznej
        class NawiazPolaczenieStatus 
        {
            public TcpClient Polaczenie { get; set; }
            public string Guid { get; set; }
        }

    }
}
