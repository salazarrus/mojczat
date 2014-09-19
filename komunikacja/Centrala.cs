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

    class Centrala
    {
        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;

        public event NowePolaczenie NowePolaczenie;
        /// <summary>
        /// Polaczenia TCP ktore zostaly otwarte
        /// </summary>
        Dictionary<IPAddress, TcpClient> otwartePolaczenia = new Dictionary<IPAddress, TcpClient>();

        /// <summary>
        /// Strumienie ktore zostaly otwarte
        /// </summary>
        Dictionary<IPAddress, Stream> otwarteStrumienie = new Dictionary<IPAddress, Stream>();
        
        Dictionary<string, IPAddress> ID_IP;
        Dictionary<IPAddress, String> IP_ID;
        
        int port;

        const int POLOCZENIE_TIMEOUT = 1000;

        public Stream this[String idUzytkownika]
        {
            get { return this[ID_IP[idUzytkownika]]; }
        }

        public Stream this[IPAddress idUzytkownika]
        {
            get
            {
                if (!otwarteStrumienie.ContainsKey(idUzytkownika)) { return null; }
                return otwarteStrumienie[idUzytkownika];
            }
        }

        public Centrala(Dictionary<string, IPAddress> ID_IP, Dictionary<IPAddress, String> IP_ID, int port) {
            this.ID_IP = ID_IP;
            this.IP_ID = IP_ID;
            this.port = port;
        }
                
        public void NawiazPolaczenie(String id)
        {
            // tworzymy nowe polaczenie 
            Trace.TraceInformation("nawiazujemy polaczenie");
            var klient = new TcpClient();
            var wynik = klient.BeginConnect(ID_IP[id], port, new AsyncCallback(nawiazPolaczenieWynik), 
                new NawiazPolaczenieStatus() { idUzytkownika = id, polaczenie = klient });

            if (!wynik.AsyncWaitHandle.WaitOne(POLOCZENIE_TIMEOUT, true)) {
                Trace.TraceInformation("timeout nawiaz polaczenie");
            }
        }

        public IPAddress CzekajNaPolaczenie(TcpListener serwer) {
            TcpClient polaczenie = serwer.AcceptTcpClient();
            Trace.TraceInformation("przyszlo nowe polaczenie)");
            var punktKontaktu = (IPEndPoint)polaczenie.Client.RemoteEndPoint;
            if (this[punktKontaktu.Address] != null) {
                Trace.TraceInformation("mamy juz takie polaczenie");
                polaczenie.Close();
                return null; 
            }
            
            var strumien = dajStrumienJakoSerwer(polaczenie);// otworz strumien dla wiadomosci
            zachowajPolaczenie(punktKontaktu.Address, polaczenie, strumien, false); // zatrzymujemy referencje  
            if (ZmianaStanuPolaczenia != null)
            {
                ZmianaStanuPolaczenia(IP_ID[punktKontaktu.Address], true);
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
                otwartePolaczenia[ipUzytkownika].Close();
                otwartePolaczenia.Remove(ipUzytkownika);
            }

            if (otwarteStrumienie.ContainsKey(ipUzytkownika))
            {
                otwarteStrumienie[ipUzytkownika].Close();
                otwarteStrumienie.Remove(ipUzytkownika);
            }
        }

        /// <summary>
        /// Rozlacz wszystkie polaczenia
        /// </summary>
        public void RozlaczWszystkich() {
            otwartePolaczenia.Keys.ToList().ForEach(i => ZamknijPolaczenie(i));
        }
        
        protected virtual Stream dajStrumienJakoKlient(TcpClient polaczenie)
        {
            return polaczenie.GetStream();
        }

        protected virtual Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        {
            return polaczenie.GetStream();
        }

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

                zachowajPolaczenie(ID_IP[status.idUzytkownika], status.polaczenie, 
                    dajStrumienJakoKlient(status.polaczenie), true);
                
                if (NowePolaczenie != null){ NowePolaczenie(status.idUzytkownika); }

                if (ZmianaStanuPolaczenia != null)
                {
                    ZmianaStanuPolaczenia(status.idUzytkownika, true);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("[nawiazPolaczenieWynik] " + ex.ToString());
            }
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
