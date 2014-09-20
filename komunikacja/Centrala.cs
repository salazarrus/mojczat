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
    public delegate void NowePolaczenie(string idUzytkownika);
    public delegate void ZamknietoPoloczenie(string idUzytkownika);

    class Centrala
    {       
        /// <summary>
        /// Polaczenia TCP ktore zostaly otwarte
        /// </summary>
        Dictionary<IPAddress, TcpClient> otwartePolaczenia = new Dictionary<IPAddress, TcpClient>();

        /// <summary>
        /// Strumienie ktore zostaly otwarte
        /// </summary>
        Dictionary<IPAddress, Stream> otwarteStrumienie = new Dictionary<IPAddress, Stream>();
        
        int port;
        Mapownik mapownik;
        const int POLOCZENIE_TIMEOUT = 1000;

        public Stream this[String idUzytkownika]
        {
            get { return this[mapownik[idUzytkownika]]; }
        }

        public Stream this[IPAddress idUzytkownika]
        {
            get
            {
                if (!otwarteStrumienie.ContainsKey(idUzytkownika)) { return null; }
                return otwarteStrumienie[idUzytkownika];
            }
        }

        public Centrala(Mapownik mapownik, int port) {
            this.mapownik = mapownik;
            this.port = port;
        }

        public event NowePolaczenie NowePolaczenieDoNas;

        public event NowePolaczenie NowePolaczenieOdNas;

        public event ZamknietoPoloczenie ZamknietoPolaczenie;

        public void ToNieDziala(string idUzytkownika) {
            ZamknijPolaczenie(mapownik[idUzytkownika]);
        }
                
        public void Polacz(String id)
        {
            // tworzymy nowe polaczenie 
            Trace.TraceInformation("nawiazujemy polaczenie");
            var klient = new TcpClient();
            int portJego;
            int.TryParse(ConfigurationManager.AppSettings["portJego"], out portJego);
            var wynik = klient.BeginConnect(mapownik[id], portJego, new AsyncCallback(nawiazPolaczenieWynik), 
                new NawiazPolaczenieStatus() { idUzytkownika = id, polaczenie = klient });
            
            if (!wynik.AsyncWaitHandle.WaitOne(POLOCZENIE_TIMEOUT, true)) {
                Trace.TraceInformation("timeout nawiaz polaczenie");
            }
        }

        public IPAddress ZajmijSiePolaczeniem(TcpClient polaczenie) 
        {            
            Trace.TraceInformation("przyszlo nowe polaczenie)");
            var punktKontaktu = (IPEndPoint)polaczenie.Client.RemoteEndPoint;
            if (this[punktKontaktu.Address] != null) {
                Trace.TraceInformation("mamy juz takie polaczenie");
                polaczenie.Close();
                return null; 
            }
            
            var strumien = dajStrumienJakoSerwer(polaczenie);// otworz strumien dla wiadomosci
            zachowajPolaczenie(punktKontaktu.Address, polaczenie, strumien, false); // zatrzymujemy referencje  
            if (NowePolaczenieDoNas != null)
            {
                NowePolaczenieDoNas(mapownik[punktKontaktu.Address]);
            }
            return punktKontaktu.Address;
        }

        /// <summary>
        /// Zwalniamy zasoby
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void ZamknijPolaczenie(IPAddress ipUzytkownika)
        {
            if (otwartePolaczenia.ContainsKey(ipUzytkownika))
            {
                try { otwartePolaczenia[ipUzytkownika].Close(); } catch { }
                otwartePolaczenia.Remove(ipUzytkownika);
            }

            if (otwarteStrumienie.ContainsKey(ipUzytkownika))
            {
                try { otwarteStrumienie[ipUzytkownika].Close(); } catch { }
                otwarteStrumienie.Remove(ipUzytkownika);
            }
            if (ZamknietoPolaczenie != null)
            {
                ZamknietoPolaczenie(mapownik[ipUzytkownika]);
            }
        }

        /// <summary>
        /// Rozlacz wszystkie polaczenia
        /// </summary>
        public void RozlaczWszystkich() 
        { otwartePolaczenia.Keys.ToList().ForEach(i => ZamknijPolaczenie(i)); }
        
        protected virtual Stream dajStrumienJakoKlient(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

        protected virtual Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        { return polaczenie.GetStream(); }

        void nawiazPolaczenieWynik(IAsyncResult wynik)
        {
            try
            {
                NawiazPolaczenieStatus status = (NawiazPolaczenieStatus)wynik.AsyncState;
                status.polaczenie.EndConnect(wynik);
                
                if (!status.polaczenie.Connected || this[status.idUzytkownika] != null)
                {
                    status.polaczenie.Close();
                    return;
                }

                zachowajPolaczenie(mapownik[status.idUzytkownika], status.polaczenie, 
                    dajStrumienJakoKlient(status.polaczenie), true);
                
                if (NowePolaczenieOdNas != null){ NowePolaczenieOdNas(status.idUzytkownika); }
            }
            catch (Exception ex)
            { Trace.TraceInformation("[nawiazPolaczenieWynik] " + ex.ToString()); }
        }

        void zachowajPolaczenie(IPAddress ipUzytkownika, TcpClient polaczenie, Stream strumien, bool nawiaz)
        {
            Trace.TraceInformation("Zachowujemy polaczenie");
            otwartePolaczenia.Add(ipUzytkownika, polaczenie);
            otwarteStrumienie.Add(ipUzytkownika, strumien);
        }

        class NawiazPolaczenieStatus {
            public TcpClient polaczenie { get; set; }
            public string idUzytkownika { get; set; }
        }
    }
}
