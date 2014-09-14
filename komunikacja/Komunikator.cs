using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace MojCzat.komunikacja
{    
    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(String id, String wiadomosc);

    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika, bool polaczenieOtwarte);

    /// <summary>
    /// Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    /// </summary>
    public class Komunikator
    {
        /// <summary>
        /// Ile bajtow mozna przeslac w jednej wiadomosci
        /// </summary>
        const int rozmiarBufora = 1024;

        /// <summary>
        /// Do kazdego kontaktu jest przypisany bufor na przesylane przez niego wiadomosci
        /// </summary>
        Dictionary<string, byte[]> buforWiadomosci = new Dictionary<string, byte[]>();
        
        /// <summary>
        /// Mapowanie adresu IP do Identyfikatora rozmowcy
        /// </summary>
        Dictionary<IPAddress, string> mapa_IP_ID;
        
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPEndPoint> mapa_ID_PunktKontaktu;
        
        /// <summary>
        /// Polaczenia TCP ktore zostaly otwarte
        /// </summary>
        Dictionary<string, TcpClient> otwartePolaczenia = new Dictionary<string, TcpClient>();

        Dictionary<string, SslStream> otwarteStrumienie = new Dictionary<string, SslStream>();

        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;

        X509Certificate certyfikat; 

        /// <summary>
        /// Konstruktor komunikatora
        /// </summary>
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do 
        /// Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPEndPoint> mapa_ID_PunktKontaktu)
        {           
            //inicjalizacja i wypelnianie mapowan pochodnych
            this.mapa_ID_PunktKontaktu = mapa_ID_PunktKontaktu;
            this.mapa_IP_ID = new Dictionary<IPAddress, string>();

            // generuj mape mapa_IP_ID
            foreach (var i in mapa_ID_PunktKontaktu)
            {
                mapa_IP_ID.Add(i.Value.Address, i.Key);
            }
            certyfikat = new X509Certificate2("cert\\cert1.pfx", "cert1pwd");

        }

        /// <summary>
        /// Gdy nadeszla nowa wiadomosc, powiadamiamy zainteresowanych 
        /// przy pomocy tego obiektu
        /// </summary>
        public event NowaWiadomosc NowaWiadomosc;

        /// <summary>
        /// Gdy nowe polaczenie zostalo nawiazane badz stare zostalo zerwane, 
        /// powiadamy zainteresowanych przy uzyciu tego obiektu
        /// </summary>
        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;

        public void DodajKontaktDoMapy(string idUzytkownika, IPEndPoint punkKontaktu) {
            mapa_IP_ID.Add(punkKontaktu.Address, idUzytkownika);
            mapa_ID_PunktKontaktu.Add(idUzytkownika, punkKontaktu);
        }

        public void UsunKontaktZMap(string idUzytkownika, IPEndPoint punkKontaktu)
        {
            mapa_IP_ID.Remove(punkKontaktu.Address);
            mapa_ID_PunktKontaktu.Remove(idUzytkownika);
        }

        /// <summary>
        /// Polacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <returns></returns>
        public bool ZainicjujPolaczenie(string idUzytkownika) {
            try
            {
                // jesli niedostepny rzuci wyjatek
                SslStream polaczenie = dajPolaczenie(idUzytkownika);
                czekajNaWiadomosc(polaczenie, idUzytkownika); 
                return true;
            }
            catch(SocketException ex) {
                return false;
            }
        }

        public void ZamknijPolaczenie(string idUzytkownika) {
            if (!otwartePolaczenia.ContainsKey(idUzytkownika)) { return; }
            otwartePolaczenia[idUzytkownika].Close();
        }


        /// <summary>
        /// wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(String idRozmowcy, String wiadomosc) { 
            try
            {
                SslStream strumien = dajPolaczenie(idRozmowcy);

                // tranformacja tekstu w bajty
                Byte[] bajty = System.Text.Encoding.UTF8.GetBytes(wiadomosc);         
                // wysylanie bajtow polaczeniem TCP 
                strumien.Write(bajty, 0, bajty.Length);
            }
            catch (SocketException ex)
            {
                //TODO cos z tym zrobic
            }
        }

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Start() 
        {           
            try
            {
                // konfiguracja nasluchu
                int port = 0;
                int.TryParse(ConfigurationManager.AppSettings["port"], out port);
                IPAddress ipSerwera = IPAddress.Parse(ConfigurationManager.AppSettings["ip"]);

                // stworz serwer
                serwer = new TcpListener(ipSerwera, port);
                //uruchom serwer
                serwer.Start();
    
                // zapetlamy
                while (true)
                {
                    // czekaj na przychodzace polaczenia
                    TcpClient polaczenie = serwer.AcceptTcpClient();
                    ZajmijSieKlientem(polaczenie);
                }
            }
            
            catch (SocketException e)
            {
                //TODO zdobic cos z tym
            }
            finally
            {
                // zatrzymaj serwer
                serwer.Stop();
            }
        }

        /// <summary>
        /// zatrzymaj serwer
        /// </summary>
        public void Stop() {
            // zatrzymaj nasluch
            if (serwer != null) { serwer.Stop(); }
            
            // zamknij otwarte polaczenia
            foreach(var polaczenie in otwartePolaczenia){
                polaczenie.Value.Close();
            }
        }

        bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaWiadomosc(SslStream strumien, string idRozmowcy)
        {
            strumien.BeginRead(buforWiadomosci[idRozmowcy], 0,
                rozmiarBufora, new AsyncCallback(zdarzenieNowaWiadomosc), idRozmowcy);
        }

        void ZajmijSieKlientem(TcpClient polaczenie) {            
            var puntkKontaktuKlienta = (IPEndPoint)polaczenie.Client.RemoteEndPoint;

            String nadawca = mapa_IP_ID.ContainsKey(puntkKontaktuKlienta.Address) ?
                mapa_IP_ID[puntkKontaktuKlienta.Address] : null;

            // nieznajomy
            if (nadawca == null)
            {
                polaczenie.Close();
                return;
            }

            // sprawdzy czy polaczenie z tego punktu kontaktu istnieje. Zamknij je.
            if (nadawca != null && otwartePolaczenia.ContainsKey(nadawca))
            {
                otwartePolaczenia[nadawca].Close();
            }

            // sprawdz czy bufor wiadmosci dla tego punktu kontatku istnieje. Jesli nie, stworz.
            if (nadawca != null && !buforWiadomosci.ContainsKey(nadawca))
            {
                buforWiadomosci.Add(nadawca, new byte[rozmiarBufora]);
            }
                        
            var strumien = new SslStream(polaczenie.GetStream(), false);
            strumien.AuthenticateAsServer(certyfikat, false, System.Security.Authentication.SslProtocols.Tls, false);

            // zachowaj to polaczenie na pozniej
            otwartePolaczenia.Add(nadawca, polaczenie);

            otwarteStrumienie.Add(nadawca, strumien);

            // powiadom zainteresowanych o nowym polaczeniu
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(nadawca, true); }

            // czekaj (pasywnie) na wiadomosc z tego polaczenia
            czekajNaWiadomosc(strumien, nadawca);
        }

        /// <summary>
        /// Znajdz otwarte polaczenie lub otworz nowe
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika do ktorego chcemy polaczenia</param>
        /// <returns> polaczenie do uzytkownika</returns>
        SslStream dajPolaczenie(string idUzytkownika)
        {
            IPEndPoint punktKontaktu = mapa_ID_PunktKontaktu[idUzytkownika];
            SslStream strumien = null;

            // sprawdz, czy to polaczenie nie jest juz otwarte
            if (otwarteStrumienie.ContainsKey(idUzytkownika))
            {
                strumien = otwarteStrumienie[idUzytkownika];
            }
            else
            {
                // tworzymy nowe polaczenie 
                var klient = new TcpClient(new IPEndPoint(
                    IPAddress.Parse(ConfigurationManager.AppSettings["ip"]), new Random().Next(10000,20000)));
                klient.Connect(punktKontaktu);

                strumien = new SslStream(klient.GetStream(), true, new
                  RemoteCertificateValidationCallback(ValidateServerCertificate));
                string host = ((IPEndPoint)klient.Client.RemoteEndPoint).Address.ToString();
                strumien.AuthenticateAsClient(host);
                
                // zachowujemy nowe polaczenie na pozniej
                otwartePolaczenia.Add(idUzytkownika, klient);

                otwarteStrumienie.Add(idUzytkownika, strumien);
            }

            //utworz bufor dla tego polaczenia, jesli jeszcze nie istnieje
            if (idUzytkownika != null && !buforWiadomosci.ContainsKey(idUzytkownika))
            {
                buforWiadomosci.Add(idUzytkownika, new byte[rozmiarBufora]);
            }

            return strumien;
        }



        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void zdarzenieNowaWiadomosc(IAsyncResult wynik)
        {         
            // od kogo przyszla wiadomosc
            var nadawca = (string)wynik.AsyncState;
            
            int index = Array.FindIndex(buforWiadomosci[nadawca], x=> x==0);
            if (index == 0) // polaczenie zostalo zamkniete 
            {
                otwartePolaczenia.Remove(nadawca);
                if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(nadawca, false); }
                return;
            }
            // dekodujemy wiadomosc
            // usuwamy \0 z konca lancucha
            string wiadomosc = index > 0 ?
                Encoding.UTF8.GetString(buforWiadomosci[nadawca], 0, index) :
                Encoding.UTF8.GetString(buforWiadomosci[nadawca]);

            // czyscimy bufor
            Array.Clear(buforWiadomosci[nadawca], 0, rozmiarBufora); 
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if(NowaWiadomosc != null){
                // informujemy zainteresowanych
                NowaWiadomosc(nadawca, wiadomosc);
            }
            // zakoncz operacje asynchroniczna
            var strumien = otwarteStrumienie[nadawca] ;
            strumien.EndRead(wynik);
            // rozpocznij nowa operacje asynchroniczna - czekaj na nowa wiadomosc
            czekajNaWiadomosc(strumien, nadawca); 
        }
    }
}
