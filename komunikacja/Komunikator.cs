﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MojCzat.komunikacja
{    
    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(String id, String wiadomosc);

    /// <summary>
    /// Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    /// </summary>
    public class Komunikator
    {
        const int rozmiarBufora = 1024;

        /// <summary>
        /// Do kazdego polaczenia tcp jest przypisany bufor na otrzymywane wiadomosci
        /// </summary>
        Dictionary<string, byte[]> buforWiadomosci = new Dictionary<string, byte[]>();
        
        /// <summary>
        /// Mapowanie adresu IP do Identyfikatora rozmowcy
        /// </summary>
        Dictionary<IPAddress, string> ipNaId;
        
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPEndPoint> idNaIpep;
        
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
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPEndPoint> idNaIpep)
        {           
            //inicjalizacja i wypelnianie mapowan pochodnych
            this.idNaIpep = idNaIpep;
            this.ipNaId = new Dictionary<IPAddress, string>();

            foreach (var i in idNaIpep)
            {
                ipNaId.Add(i.Value.Address, i.Key);
            }
        }

        /// <summary>
        /// Gdy nadeszla nowa wiadomosc, powiadamiamy zainteresowanych przy pomocy tego obiektu
        /// </summary>
        public event NowaWiadomosc NowaWiadomosc;

        /// <summary>
        /// wyslij wiadomosc tekstowa do innego
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void Pisz(String idRozmowcy, String wiadomosc) { 
            try
            {
                // wyszuaj adres IP i Port rozmowcy
                IPEndPoint punktKontatku = idNaIpep[idRozmowcy];
                TcpClient polaczenie;
                
                // sprawdz, czy to polaczenie nie jest juz otwarte
                if (otwartePolaczenia.ContainsKey(idRozmowcy))
                {
                    polaczenie = otwartePolaczenia[idRozmowcy]; 
                }
                else 
                {
                    // tworzymy nowe polaczenie 
                    polaczenie = new TcpClient();
                    polaczenie.Connect(punktKontatku);
                    // zachowujemy nowe polaczenie na pozniej
                    otwartePolaczenia.Add(idRozmowcy, polaczenie);
                }
                if (idRozmowcy != null && !buforWiadomosci.ContainsKey(idRozmowcy))
                {
                    buforWiadomosci.Add(idRozmowcy, new byte[rozmiarBufora]);
                }

                // tranformacja tekstu w bajty
                Byte[] bajty = System.Text.Encoding.ASCII.GetBytes(wiadomosc);         
                // wysylanie bajtow polaczeniem TCP 
                polaczenie.GetStream().Write(bajty, 0, bajty.Length);
                polaczenie.GetStream().BeginRead(buforWiadomosci[idRozmowcy], 0,
                        rozmiarBufora, new AsyncCallback(zdarzenieNowaWiadomosc), idRozmowcy); 

            }
            catch (SocketException ex)
            {
                //TODO cos z tym zrobic
            }
        }

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Sluchaj() {
           
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

                    String idNadawcy = ipNaId.ContainsKey(puntkKontaktuKlienta.Address) ?
                        ipNaId[puntkKontaktuKlienta.Address] : null;
                    // sprawdzy czy polaczenie z tego punktu kontaktu istnieje. Zamknij je.
                    if (idNadawcy != null && otwartePolaczenia.ContainsKey(idNadawcy)) 
                    {
                        otwartePolaczenia[idNadawcy].Close();
                    }
                    // sprawdz czy bufor wiadmosci dla tego punktu kontatku istnieje. Jesli nie, stworz.
                    if (idNadawcy != null && !buforWiadomosci.ContainsKey(idNadawcy))
                    {
                        buforWiadomosci.Add(idNadawcy, new byte[rozmiarBufora]);
                    }

                    // zachowaj to polaczenie na pozniej
                    otwartePolaczenia.Add(idNadawcy, polaczenie);

                    // sprawdzamy, od ktos sie z nami polaczyl
                    string idRozmowcy = ipNaId.ContainsKey(puntkKontaktuKlienta.Address) ?
                    ipNaId[puntkKontaktuKlienta.Address] : null;
                    
                    // czekaj (pasywnie) na nadchodzace wiadomosci
                    polaczenie.GetStream().BeginRead(buforWiadomosci[idNadawcy], 0,
                        rozmiarBufora, new AsyncCallback(zdarzenieNowaWiadomosc), idRozmowcy); 
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
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void zdarzenieNowaWiadomosc(IAsyncResult wynik)
        {         
            // od kogo przyszla wiadomosc
            var nadawca = (string)wynik.AsyncState;
            // dekodujemy wiadomosc
            // usuwamy \0 z konca lancucha
            int index = Array.FindIndex(buforWiadomosci[nadawca], x=> x==0);
            string wiadomosc = index > 0 ?
                Encoding.ASCII.GetString(buforWiadomosci[nadawca], 0, index) :
                Encoding.ASCII.GetString(buforWiadomosci[nadawca]);

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
            otwartePolaczenia[nadawca].GetStream()
                .BeginRead(buforWiadomosci[nadawca],
                0, rozmiarBufora, new AsyncCallback(zdarzenieNowaWiadomosc), nadawca); 
        }
    }
}