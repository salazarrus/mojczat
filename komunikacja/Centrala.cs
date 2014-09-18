using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MojCzat.komunikacja
{
    class Centrala
    {
        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;

        public event ZmianaStanuPolaczenia NawiazalismyPolaczenie;
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

        public Stream this[String idUzytkownika]
        {
            get {
                IPAddress cel = ID_IP[idUzytkownika];

                // sprawdz, czy to polaczenie nie jest juz otwarte
                return this[cel];
            }
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

        public IPAddress dajIp(string idUzytkownika)
        {
            return ID_IP[idUzytkownika];
        }    
                
        public void NawiazPolaczenie(String id)
        {
            //MessageBox.Show("nazwiazujemy");
            // tworzymy nowe polaczenie 
            var klient = new TcpClient();
            
            var wynik = klient.BeginConnect(ID_IP[id], port,
                new AsyncCallback(nawiazPolaczenieWynik), new r1() { id = id, c = klient });

            var notimeout = wynik.AsyncWaitHandle.WaitOne(1000, true);
            if (!notimeout) {
                //MessageBox.Show("Timeout");
                //klient.EndConnect(wynik);
            }
            
        }

        void nawiazPolaczenieWynik(IAsyncResult wynik) {
            try{
                r1 r = (r1)wynik.AsyncState;
                r.c.EndConnect(wynik);
                Stream strumien;
                
                if(!r.c.Connected){
                    MessageBox.Show("Closing failed connection.");
                    r.c.Close();
                    return; 
                }
                if (this[r.id] != null) 
                {
                    r.c.Close();
                    return;
                }
                strumien = dajStrumienJakoKlient(r.c);
                        
                // zachowujemy nowe polaczenie na pozniej

                ZachowajPolaczenie(ID_IP[r.id], r.c, strumien, true);
                if (NawiazalismyPolaczenie != null) {
                    NawiazalismyPolaczenie(r.id, true);
                }

                if(ZmianaStanuPolaczenia != null)
                {
                    ZmianaStanuPolaczenia(r.id, true);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }

        public IPAddress CzekajNaPolaczenie(TcpListener serwer) {
            TcpClient polaczenie = serwer.AcceptTcpClient();
            //MessageBox.Show("czekaj");
            var punktKontaktu = (IPEndPoint)polaczenie.Client.RemoteEndPoint;
            if (this[punktKontaktu.Address] != null) { return punktKontaktu.Address; }

            // posprzataj stare polaczenie
            //ZamknijPolaczenie(punktKontaktu.Address);

            var strumien = dajStrumienJakoSerwer(polaczenie);// otworz strumien dla wiadomosci
            ZachowajPolaczenie(punktKontaktu.Address, polaczenie, strumien, false); // zatrzymujemy referencje  
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
        public void Rozlacz() {
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

        void ZachowajPolaczenie(IPAddress ipUzytkownika, TcpClient polaczenie, Stream strumien, bool nawiaz)
        {
            MessageBox.Show("zachowujemy polaczenie " + nawiaz);
            otwartePolaczenia.Add(ipUzytkownika, polaczenie);
            otwarteStrumienie.Add(ipUzytkownika, strumien);
        }

        class r1 {
            public TcpClient c { get; set; }
            public string id { get; set; }

        
        }
    }
}
