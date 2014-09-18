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
using MojCzat.model;

namespace MojCzat.komunikacja
{    
    public enum Wiadomosc{
        Zwykla,
        Opis
    }

    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(String id, Wiadomosc rodzaj , String wiadomosc);

    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika, bool polaczenieOtwarte);

    /// <summary>
    /// Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    /// </summary>
    public class Komunikator
    {
     
        
        /// <summary>
        /// Na jakim porcie nasluchujemy wiadomosci
        /// </summary>
        int port;
            
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPAddress> ID_IP;

        Dictionary<IPAddress, string> IP_ID; 

        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;

        /// <summary>
        /// Obiekt odpowiedzialny za laczenie się z innymi uzytkownikami
        /// </summary>
        Centrala centrala;

        Dictionary<string, Queue<byte[]>> komunikatyWychodzace = new Dictionary<string, Queue<byte[]>>();
        Dictionary<string, Boolean> wysylanieWToku = new Dictionary<string,Boolean>();
        Dictionary<string, object> zamkiWysylania = new Dictionary<string, object>();

        public String opis = "";

        Buforownia buforownia = new Buforownia();

        /// <summary>
        /// Konstruktor komunikatora
        /// </summary>
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do 
        /// Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPAddress> mapa_ID_PunktKontaktu, Ustawienia ustawienia)
        {
            const int portBezSSL = 5080;
            const int portSSL = 5443;

            //inicjalizacja i wypelnianie mapowan pochodnych
            this.ID_IP = mapa_ID_PunktKontaktu;
                        
            // generuj mape mapa_IP_ID
            this.IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in mapa_ID_PunktKontaktu) { 
                IP_ID.Add(i.Value, i.Key);
                wysylanieWToku.Add(i.Key, false);
                zamkiWysylania.Add(i.Key, new object());
            }
                

            if(ustawienia.SSLWlaczone)
            {
                centrala = new CentralaSSL(ustawienia.Certyfikat) ;
                port = portSSL;
            }
            else
            {
                centrala = new Centrala();
                port = portBezSSL; 
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

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <param name="punktKontaktu"></param>
        public void DodajKontakt(string idUzytkownika, IPAddress punktKontaktu)
        {
            IP_ID.Add(punktKontaktu, idUzytkownika);
            ID_IP.Add(idUzytkownika, punktKontaktu);
            zamkiWysylania.Add(idUzytkownika, new object());
            wysylanieWToku.Add(idUzytkownika, false);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void UsunKontakt(string idUzytkownika)
        {
            IP_ID.Remove(ID_IP[idUzytkownika]);
            ID_IP.Remove(idUzytkownika);
            zamkiWysylania.Remove(idUzytkownika);
            wysylanieWToku.Remove(idUzytkownika);
            buforownia.Usun(idUzytkownika);
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
                wyslijOpis(idUzytkownika);
                poprosOpis(idUzytkownika);
                czekajNaZapytanie(idUzytkownika); 
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
        public bool WyslijWiadomosc(String idRozmowcy, String wiadomosc) {
            try
            {
                Stream strumien = dajStrumien(idRozmowcy);
                // tranformacja tekstu w bajty
                
                Byte[] bajty = stworzKomunikat(Protokol.ZwyklaWiadomosc, wiadomosc);  
                // wysylanie bajtow polaczeniem TCP 
                dajKolejkeWiadomosci(idRozmowcy).Enqueue(bajty);
                wysylajZKolejki(idRozmowcy);
                return true;// TODO zmienic
            }
            catch { return false; }           
        }

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Start() 
        {           
            try
            {
                serwer = new TcpListener(IPAddress.Any, port); // stworz serwer
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

        public void ZautualizujOpis() {
            ID_IP.Keys.ToList().ForEach(s => wyslijOpis(s));
        }

        public void poprosOpis(string id)
        {
            var bajty = stworzKomunikat(Protokol.DajMiSwojOpis, "");
            dajKolejkeWiadomosci(id).Enqueue(bajty);
            wysylajZKolejki(id);
        }

        IPAddress dajIp(string idUzytkownika) {
            return ID_IP[idUzytkownika];
        }

        byte[] stworzKomunikat(byte rodzaj , string wiadomosc) {
            wiadomosc = Encoding.UTF8.GetString(new byte[] { rodzaj }) + wiadomosc;
            return  Encoding.UTF8.GetBytes(wiadomosc);  
        }
   
        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaZapytanie(string idRozmowcy)
        {
            var wynik = new StatusObsluzZapytanie(){ idNadawcy=idRozmowcy, typ=new byte[1]};
            centrala[dajIp(idRozmowcy)].BeginRead(wynik.typ, 0, 1, obsluzZapytanie, wynik);

        }

        void obsluzZapytanie(IAsyncResult wynik){
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            if (centrala[dajIp(status.idNadawcy)] == null) { return; }

            try
            {
                centrala[dajIp(status.idNadawcy)].EndRead(wynik);
            }
            catch { // zostalismy rozlaczeni
                return;
            }
            
            var rodzaj = Encoding.UTF8.GetString(status.typ);

            switch (status.typ[0]) { 
                case Protokol.KoniecPolaczenia:
                    Rozlacz(status.idNadawcy);
                    if (ZmianaStanuPolaczenia != null) 
                    {
                        ZmianaStanuPolaczenia(status.idNadawcy, false); 
                    }
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    czytajWiadomosc(status.idNadawcy, Wiadomosc.Zwykla);
                    break;
                case Protokol.DajMiSwojOpis:
                    wyslijOpis(status.idNadawcy);
                    czekajNaZapytanie(status.idNadawcy);
                    break;
                case Protokol.OtoMojOpis:
                    czytajWiadomosc(status.idNadawcy, Wiadomosc.Opis);
                    break;
                default:
                    czekajNaZapytanie(status.idNadawcy);
                    break;
            }          
        }


        void czytajWiadomosc(string id, Wiadomosc rodzaj) { 
            centrala[dajIp(id)].BeginRead(buforownia[id], 0,
                Buforownia.RozmiarBufora, new AsyncCallback(obsluzWiadomosc), 
                new CzytajWiadomoscStatus(){id= id, rodzaj= rodzaj} );
        }


        void wyslijOpis(string id) {
            dajKolejkeWiadomosci(id).Enqueue(stworzKomunikat(Protokol.OtoMojOpis, opis));
            wysylajZKolejki(id);
        }

        void wysylajZKolejki(string idUzytkownika) {
            byte[] doWyslania = null;
            lock (zamkiWysylania[idUzytkownika]) {
                if (wysylanieWToku[idUzytkownika]) {
                    return;
                }

                if (dajKolejkeWiadomosci(idUzytkownika).Any()) {
                    doWyslania = dajKolejkeWiadomosci(idUzytkownika).Dequeue();
                    wysylanieWToku[idUzytkownika] = true;
                }
            }

            if (doWyslania != null) {
                centrala[dajIp(idUzytkownika)].BeginWrite
                          (doWyslania, 0, doWyslania.Length, new AsyncCallback(komunikatWyslany), idUzytkownika);
            }
        }

        void komunikatWyslany(IAsyncResult wynik) {            
            var id = (string)wynik.AsyncState;
            centrala[dajIp(id)].EndWrite(wynik);
        
            lock (zamkiWysylania[id]) {
                wysylanieWToku[id] = false;    
            }
            wysylajZKolejki(id);
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
            
            // powiadom zainteresowanych o nowym polaczeniu
            if (ZmianaStanuPolaczenia != null)
            { ZmianaStanuPolaczenia(nadawca, true); }

            czekajNaZapytanie(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        }

      
        /// <summary>
        /// Nadeszla nowa wiadomosc
        /// </summary>
        /// <param name="wynik"> obiektu tego uzywamy do zakonczenia jednej 
        /// operacji asynchronicznej i rozpoczecia nowej </param>
        void obsluzWiadomosc(IAsyncResult wynik)
        {   
            // od kogo przyszla wiadomosc
            var status = (CzytajWiadomoscStatus)wynik.AsyncState;
            // zakoncz operacje asynchroniczna
            var strumien = centrala[dajIp(status.id)];
            strumien.EndRead(wynik);

            int index = Array.FindIndex(buforownia[status.id], x=> x==0);
            // dekodujemy wiadomosc
            // usuwamy \0 z konca lancucha
            string wiadomosc = index >= 0 ?
                Encoding.UTF8.GetString(buforownia[status.id], 0, index) :
                Encoding.UTF8.GetString(buforownia[status.id]);

            // czyscimy bufor
            Array.Clear(buforownia[status.id], 0, Buforownia.RozmiarBufora); 
            // jesli sa zainteresowani, informujemy ich o nowej wiadomosci
            if(NowaWiadomosc != null){
                // informujemy zainteresowanych
                NowaWiadomosc(status.id, status.rodzaj , wiadomosc);
            }

            // rozpocznij nowa operacje asynchroniczna - czekaj na nowa wiadomosc
            czekajNaZapytanie(status.id); 
        }

        /// <summary>
        /// Znajdz otwarte polaczenie lub otworz nowe
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika do ktorego chcemy polaczenia</param>
        /// <returns> polaczenie do uzytkownika</returns>
        Stream dajStrumien(string idUzytkownika)
        {
            IPAddress cel = ID_IP[idUzytkownika];

            // sprawdz, czy to polaczenie nie jest juz otwarte
            if (centrala[cel] != null) { return centrala[cel]; }

            var polaczenie = centrala.NawiazPolaczenie(new IPEndPoint(cel, port));
            return polaczenie;
        }

        Queue<byte[]> dajKolejkeWiadomosci(string id)
        {
            if (!komunikatyWychodzace.ContainsKey(id))
            {
                komunikatyWychodzace.Add(id, new Queue<byte[]>());
            }
            return komunikatyWychodzace[id];
        }
        
        class CzytajWiadomoscStatus
        {
            public String id { get; set; }
            public Wiadomosc rodzaj { get; set; }
        }


        class StatusObsluzZapytanie
        {
            public byte[] typ;
            public String idNadawcy;
        }



    }
}
