#define TRACE

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
    
    class Centrala
    {       
        /// <summary>
        /// Polaczenia TCP ktore zostaly otwarte
        /// </summary>
        Dictionary<string, TcpClient> otwartePolaczenia = new Dictionary<string, TcpClient>();

        /// <summary>
        /// Strumienie ktore zostaly otwarte
        /// </summary>
        Dictionary<string, Stream> otwarteStrumienie = new Dictionary<string, Stream>();

        // obiekt nasluchujacy nadchodzacych polaczen
        TcpListener serwer;

        const int POLOCZENIE_TIMEOUT = 1000;

        protected virtual int Port { get { return 5080; } }

        public event OtwartoPolaczenie OtwartoPolaczenie;

        public event ZamknietoPoloczenie ZamknietoPolaczenie;

        // Oczekuj nadchodzacych polaczen
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

        // zatrzymaj nasluch
        public void Stop() 
        { 
            if (serwer != null) { 
                serwer.Stop();
                RozlaczWszystkich();
            } 
        }

        public void ToNieDziala(string guid) {
            Rozlacz(guid);
        }
                
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
        /// Zwalniamy zasoby
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void Rozlacz(string guid)
        {
            if (otwartePolaczenia.ContainsKey(guid))
            {
                try { otwartePolaczenia[guid].Close(); } catch { }
                otwartePolaczenia.Remove(guid);
            }

            if (otwarteStrumienie.ContainsKey(guid))
            {
                try { otwarteStrumienie[guid].Close(); } catch { }
                otwarteStrumienie.Remove(guid);
            }
            if (ZamknietoPolaczenie != null) { ZamknietoPolaczenie(guid); }
        }

        /// <summary>
        /// Rozlacz wszystkie polaczenia
        /// </summary>
        public void RozlaczWszystkich() 
        { otwartePolaczenia.Keys.ToList().ForEach(i => Rozlacz(i)); }
        
        protected virtual Stream dajStrumienJakoKlient(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

        protected virtual Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

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

        void zachowajNowePolaczenie(TcpClient polaczenie, string guid ,Stream strumien)
        {
            Trace.TraceInformation("Zachowujemy polaczenie");

            otwartePolaczenia.Add(guid, polaczenie);
            otwarteStrumienie.Add(guid, strumien);
            var ip = ((IPEndPoint)polaczenie.Client.RemoteEndPoint).Address;

            if (OtwartoPolaczenie != null) { OtwartoPolaczenie(guid, strumien, ip); }
        }

        class NawiazPolaczenieStatus 
        {
            public TcpClient Polaczenie { get; set; }
            public string Guid { get; set; }
        }

    }
}
