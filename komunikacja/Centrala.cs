using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MojCzat.komunikacja
{
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

        public Stream this[IPAddress idUzytkownika]
        {
            get
            {
                if (!otwarteStrumienie.ContainsKey(idUzytkownika)) { return null; }
                return otwarteStrumienie[idUzytkownika];
            }
        }
                
        public Stream NawiazPolaczenie(IPEndPoint punktKontaktu)
        {
            Stream strumien;

            // tworzymy nowe polaczenie 
            var polaczanie = new TcpClient(new IPEndPoint(
                IPAddress.Parse(ConfigurationManager.AppSettings["ip"]),
                new Random().Next(10000, 20000)));
            polaczanie.Connect(punktKontaktu);

            strumien = dajStrumienJakoKlient(polaczanie);
            // zachowujemy nowe polaczenie na pozniej
            ZachowajPolaczenie(punktKontaktu.Address, polaczanie, strumien);

            return strumien;
        }

        public IPAddress CzekajNaPolaczenie(TcpListener serwer) {
            TcpClient polaczenie = serwer.AcceptTcpClient();

            var punktKontaktu = (IPEndPoint)polaczenie.Client.RemoteEndPoint;

            // posprzataj stare polaczenie
            ZamknijPolaczenie(punktKontaktu.Address);

            var strumien = dajStrumienJakoSerwer(polaczenie);// otworz strumien dla wiadomosci
            ZachowajPolaczenie(punktKontaktu.Address, polaczenie, strumien); // zatrzymujemy referencje  

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

        void ZachowajPolaczenie(IPAddress ipUzytkownika, TcpClient polaczenie, Stream strumien)
        {
            otwartePolaczenia.Add(ipUzytkownika, polaczenie);
            otwarteStrumienie.Add(ipUzytkownika, strumien);
        }
    }
}
