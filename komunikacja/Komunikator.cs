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
using System.IO;
using System.Security.Authentication;

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
        /// Strumienie ktore zostaly otwarte
        /// </summary>
        Dictionary<string, Stream> otwarteStrumienie = new Dictionary<string, Stream>();

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
                        
            // generuj mape mapa_IP_ID
            this.mapa_IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in mapa_ID_PunktKontaktu) { mapa_IP_ID.Add(i.Value.Address, i.Key); }

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

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <param name="punktKontaktu"></param>
        public void DodajKontakt(string idUzytkownika, IPEndPoint punktKontaktu)
        {
            mapa_IP_ID.Add(punktKontaktu.Address, idUzytkownika);
            mapa_ID_PunktKontaktu.Add(idUzytkownika, punktKontaktu);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void UsunKontakt(string idUzytkownika)
        {
            mapa_IP_ID.Remove(mapa_ID_PunktKontaktu[idUzytkownika].Address);
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
                Stream polaczenie = dajStrumien(idUzytkownika);
                czekajNaWiadomosc(idUzytkownika); 
                return true;
            }
            catch{
                return false;
            }
        }

        /// <summary>
        /// Rozlacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void Rozlacz(string idUzytkownika) {
            zamknijPolaczenie(idUzytkownika);
            zamknijStrumien(idUzytkownika);
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(String idRozmowcy, String wiadomosc) { 
            try
            {
                Stream strumien = dajStrumien(idRozmowcy);

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
                    zajmijSieKlientem(polaczenie);
                }
            }
            
            catch
            {
                //TODO zdobic cos z tym
            }
            finally { Stop(); }
        }

        /// <summary>
        /// zatrzymaj serwer
        /// </summary>
        public void Stop() {
            // zatrzymaj nasluch
            if (serwer != null) { serwer.Stop(); }

            mapa_ID_PunktKontaktu.Keys.ToList().ForEach(id => Rozlacz(id));
        }

        protected virtual Stream dajStrumienJakoKlient(TcpClient polaczenie){
            return polaczenie.GetStream();
        }

        protected virtual Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        {
            return polaczenie.GetStream();
        }

        /// <summary>
        /// Zwalniamy zasoby
        /// </summary>
        /// <param name="idUzytkownika"></param>
        void zamknijPolaczenie(string idUzytkownika) 
        {
            if (otwartePolaczenia.ContainsKey(idUzytkownika)) 
            {
                otwartePolaczenia[idUzytkownika].Close();
                otwartePolaczenia.Remove(idUzytkownika);
            }
        }

        /// <summary>
        /// Zwalniamy zasoby
        /// </summary>
        /// <param name="idUzytkownika"></param>
        void zamknijStrumien(string idUzytkownika)
        {
            if (otwarteStrumienie.ContainsKey(idUzytkownika)) 
            { 
                otwarteStrumienie[idUzytkownika].Close();
                otwarteStrumienie.Remove(idUzytkownika);
            }
        }        

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaWiadomosc(string idRozmowcy)
        {
            otwarteStrumienie[idRozmowcy].BeginRead(buforWiadomosci[idRozmowcy], 0,
                rozmiarBufora, new AsyncCallback(obsluzNowaWiadomosc), idRozmowcy);
        }

        /// <summary>
        /// Nadeszlo polaczenie, obslugujemy je
        /// </summary>
        /// <param name="polaczenie"></param>
        void zajmijSieKlientem(TcpClient polaczenie) {            
            var punktKontaktu = (IPEndPoint)polaczenie.Client.RemoteEndPoint;

            String nadawca = mapa_IP_ID.ContainsKey(punktKontaktu.Address) ?
                mapa_IP_ID[punktKontaktu.Address] : null;

            // Nieznajomy? Do widzenia.
            if (nadawca == null)
            {
                polaczenie.Close();
                return;
            }

            Rozlacz(nadawca); // jesli bylismy polaczeni, posprzatajmy
            utworzBuforNaWiadomosci(nadawca);
            var strumien = dajStrumienJakoSerwer(polaczenie);// otworz strumien dla wiadomosci
            zachowajPolaczenie(nadawca, polaczenie, strumien); // zatrzymujemy referencje

            // powiadom zainteresowanych o nowym polaczeniu
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(nadawca, true); }

            czekajNaWiadomosc(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        }

        /// <summary>
        /// Znajdz otwarte polaczenie lub otworz nowe
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika do ktorego chcemy polaczenia</param>
        /// <returns> polaczenie do uzytkownika</returns>
        Stream dajStrumien(string idUzytkownika)
        {
            IPEndPoint punktKontaktu = mapa_ID_PunktKontaktu[idUzytkownika];
            Stream strumien = null;

            // sprawdz, czy to polaczenie nie jest juz otwarte
            if (otwarteStrumienie.ContainsKey(idUzytkownika))
            {
                strumien = otwarteStrumienie[idUzytkownika];
            }
            else
            {
                // tworzymy nowe polaczenie 
                var polaczanie = new TcpClient(new IPEndPoint(
                    IPAddress.Parse(ConfigurationManager.AppSettings["ip"]), 
                    new Random().Next(10000,20000)));
                polaczanie.Connect(punktKontaktu);

                strumien = dajStrumienJakoKlient(polaczanie);
                // zachowujemy nowe polaczenie na pozniej
                zachowajPolaczenie(idUzytkownika, polaczanie, strumien);

                //utworz bufor dla tego polaczenia, jesli jeszcze nie istnieje
                utworzBuforNaWiadomosci(idUzytkownika);
            }
           
            return strumien;
        }

        void zachowajPolaczenie(string idUzytkownika, TcpClient polaczenie, Stream strumien) 
        {
            otwartePolaczenia.Add(idUzytkownika, polaczenie);
            otwarteStrumienie.Add(idUzytkownika, strumien);
        }

        void utworzBuforNaWiadomosci(string idUzytkownika) 
        {
            if (!buforWiadomosci.ContainsKey(idUzytkownika))
            {
                buforWiadomosci.Add(idUzytkownika, new byte[rozmiarBufora]);
            }        
        }

        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void obsluzNowaWiadomosc(IAsyncResult wynik)
        {         
            // od kogo przyszla wiadomosc
            var nadawca = (string)wynik.AsyncState;
            
            int index = Array.FindIndex(buforWiadomosci[nadawca], x=> x==0);
            if (index == 0) // polaczenie zostalo zamkniete 
            {
                Rozlacz(nadawca);
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
            czekajNaWiadomosc(nadawca); 
        }
    }
}
