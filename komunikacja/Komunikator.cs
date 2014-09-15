﻿using System;
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
        Dictionary<string, byte[]> bufory = new Dictionary<string, byte[]>();
        
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPEndPoint> ID_IPEP;

        Dictionary<IPAddress, string> IP_ID; 

        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;

        /// <summary>
        /// Obiekt odpowiedzialny za laczenie się z innymi uzytkownikami
        /// </summary>
        Centrala centrala;

        /// <summary>
        /// Konstruktor komunikatora
        /// </summary>
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do 
        /// Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPEndPoint> mapa_ID_PunktKontaktu, bool uzywajSSL)
        {
            //inicjalizacja i wypelnianie mapowan pochodnych
            this.ID_IPEP = mapa_ID_PunktKontaktu;
                        
            // generuj mape mapa_IP_ID
            this.IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in mapa_ID_PunktKontaktu) { IP_ID.Add(i.Value.Address, i.Key); }

            centrala = uzywajSSL ? new CentralaSSL() : new Centrala();
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
            IP_ID.Add(punktKontaktu.Address, idUzytkownika);
            ID_IPEP.Add(idUzytkownika, punktKontaktu);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void UsunKontakt(string idUzytkownika)
        {
            IP_ID.Remove(ID_IPEP[idUzytkownika].Address);
            ID_IPEP.Remove(idUzytkownika);
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
                dajStrumien(idUzytkownika);
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
            centrala.ZamknijPolaczenie(dajIp(idUzytkownika));
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

                serwer = new TcpListener(ipSerwera, port); // stworz serwer
                serwer.Start(); //uruchom serwer

                while (true) // zapetlamy
                {
                    // czekaj na przychodzace polaczenia
                    IPAddress adresKlienta = centrala.CzekajNaPolaczenie(serwer);
                    zajmijSieKlientem(adresKlienta);
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
            centrala.Rozlacz();
        }

        IPAddress dajIp(string idUzytkownika) {
            return ID_IPEP[idUzytkownika].Address;
        }

        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaWiadomosc(string idRozmowcy)
        {
            centrala[dajIp(idRozmowcy)].BeginRead(bufory[idRozmowcy], 0,
                rozmiarBufora, new AsyncCallback(obsluzWiadomosc), idRozmowcy);
        }

        /// <summary>
        /// Nadeszlo polaczenie, obslugujemy je
        /// </summary>
        /// <param name="polaczenie"></param>
        void zajmijSieKlientem(IPAddress ipNadawcy) {            
            // Nieznajomy? Do widzenia.
            if (!IP_ID.ContainsKey(ipNadawcy)){
                centrala.ZamknijPolaczenie(ipNadawcy);
                return; 
            }
            var nadawca = IP_ID[ipNadawcy];

            utworzBuforNaWiadomosci(nadawca);
            
            // powiadom zainteresowanych o nowym polaczeniu
            if (ZmianaStanuPolaczenia != null)
            { ZmianaStanuPolaczenia(nadawca, true); }

            czekajNaWiadomosc(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        }

        /// <summary>
        /// Znajdz otwarte polaczenie lub otworz nowe
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika do ktorego chcemy polaczenia</param>
        /// <returns> polaczenie do uzytkownika</returns>
        Stream dajStrumien(string idUzytkownika)
        {
            IPEndPoint cel = ID_IPEP[idUzytkownika];

            // sprawdz, czy to polaczenie nie jest juz otwarte
            if (centrala[cel.Address] != null) { return centrala[cel.Address]; }

            utworzBuforNaWiadomosci(idUzytkownika); 
            return centrala.NawiazPolaczenie(cel);
        }
        
        void utworzBuforNaWiadomosci(string idUzytkownika) 
        {
            if (!bufory.ContainsKey(idUzytkownika))
            {
                bufory.Add(idUzytkownika, new byte[rozmiarBufora]);
            }        
        }

        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void obsluzWiadomosc(IAsyncResult wynik)
        {         
            // od kogo przyszla wiadomosc
            var nadawca = (string)wynik.AsyncState;
            
            int index = Array.FindIndex(bufory[nadawca], x=> x==0);
            if (index == 0) // polaczenie zostalo zamkniete 
            {
                Rozlacz(nadawca);
                if (ZmianaStanuPolaczenia != null) 
                { ZmianaStanuPolaczenia(nadawca, false); }
                return;
            }
            // dekodujemy wiadomosc
            // usuwamy \0 z konca lancucha
            string wiadomosc = index > 0 ?
                Encoding.UTF8.GetString(bufory[nadawca], 0, index) :
                Encoding.UTF8.GetString(bufory[nadawca]);

            // czyscimy bufor
            Array.Clear(bufory[nadawca], 0, rozmiarBufora); 
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if(NowaWiadomosc != null){
                // informujemy zainteresowanych
                NowaWiadomosc(nadawca, wiadomosc);
            }
            // zakoncz operacje asynchroniczna
            var strumien = centrala[dajIp(nadawca)] ;
            strumien.EndRead(wynik);
            // rozpocznij nowa operacje asynchroniczna - czekaj na nowa wiadomosc
            czekajNaWiadomosc(nadawca); 
        }
    }
}
