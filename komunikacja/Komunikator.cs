using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
        
        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;
        
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
                TcpClient polaczenie = dajPolaczenie(idUzytkownika);
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
                TcpClient polaczenie = dajPolaczenie(idRozmowcy);

                // tranformacja tekstu w bajty
                Byte[] bajty = System.Text.Encoding.UTF8.GetBytes(wiadomosc);         
                // wysylanie bajtow polaczeniem TCP 
                polaczenie.GetStream().Write(bajty, 0, bajty.Length);
                czekajNaWiadomosc(polaczenie, idRozmowcy);

            }
            catch (SocketException ex)
            {
                //TODO cos z tym zrobic
            }
        }

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Start() {
           
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
                    var puntkKontaktuKlienta = (IPEndPoint)polaczenie.Client.RemoteEndPoint;

                    String nadawca = mapa_IP_ID.ContainsKey(puntkKontaktuKlienta.Address) ?
                        mapa_IP_ID[puntkKontaktuKlienta.Address] : null;
                    
                    // nieznajomy
                    if (nadawca == null) {
                        polaczenie.Close();
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

                    // zachowaj to polaczenie na pozniej
                    otwartePolaczenia.Add(nadawca, polaczenie);

                    // powiadom zainteresowanych o nowym polaczeniu
                    if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(nadawca, true); }

                    // czekaj (pasywnie) na wiadomosc z tego polaczenia
                    czekajNaWiadomosc(polaczenie, nadawca);
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
            if (serwer != null) { serwer.Stop(); }
        }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaWiadomosc(TcpClient polaczenie, string idRozmowcy) {
            polaczenie.GetStream().BeginRead(buforWiadomosci[idRozmowcy], 0,
                rozmiarBufora, new AsyncCallback(zdarzenieNowaWiadomosc), idRozmowcy); 
        }

        /// <summary>
        /// Znajdz otwarte polaczenie lub otworz nowe
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika do ktorego chcemy polaczenia</param>
        /// <returns> polaczenie do uzytkownika</returns>
        TcpClient dajPolaczenie(string idUzytkownika) {
            IPEndPoint punktKontaktu = mapa_ID_PunktKontaktu[idUzytkownika];
            TcpClient polaczenie;

            // sprawdz, czy to polaczenie nie jest juz otwarte
            if (otwartePolaczenia.ContainsKey(idUzytkownika))
            {
                polaczenie = otwartePolaczenia[idUzytkownika];
            }
            else
            {
                // tworzymy nowe polaczenie 
                polaczenie = new TcpClient(new IPEndPoint(
                    IPAddress.Parse(ConfigurationManager.AppSettings["ip"]), 12345));
                polaczenie.Connect(punktKontaktu);
                // zachowujemy nowe polaczenie na pozniej
                otwartePolaczenia.Add(idUzytkownika, polaczenie);
            }

            //utworz bufor dla tego polaczenia, jesli jeszcze nie istnieje
            if (idUzytkownika != null && !buforWiadomosci.ContainsKey(idUzytkownika))
            {
                buforWiadomosci.Add(idUzytkownika, new byte[rozmiarBufora]);
            }

            return polaczenie;
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
            otwartePolaczenia[nadawca].GetStream().EndRead(wynik);
            // rozpocznij nowa operacje asynchroniczna - czekaj na nowa wiadomosc
            czekajNaWiadomosc(otwartePolaczenia[nadawca], nadawca); 
        }
    }
}
