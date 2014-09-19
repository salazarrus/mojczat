#define TRACE

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
using System.Windows.Forms;
using System.Diagnostics;

namespace MojCzat.komunikacja
{    
    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(String id, TypWiadomosci rodzaj , String wiadomosc);

    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika);

    public delegate void CzytanieSkonczone(string idUzytkownika);

    /// <summary>
    /// Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    /// </summary>
    public class Komunikator
    {
        public String Opis { get; set; }
        
        /// <summary>
        /// Na jakim porcie nasluchujemy wiadomosci
        /// </summary>
        int port;
            
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPAddress> ID_IP;

        /// <summary>
        /// Odwrotnosc mapy ID_IP
        /// </summary>
        Dictionary<IPAddress, string> IP_ID;

        /// <summary>
        /// Dostepnosc uzytkownika
        /// </summary>
        Dictionary<string, bool> dostepny;
                
        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;

        /// <summary>
        /// Obiekt odpowiedzialny za laczenie się z innymi uzytkownikami
        /// </summary>
        Centrala centrala;
                
        Buforownia buforownia = new Buforownia();

        Wiadomosciownia wiadomosciownia;

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
            this.IP_ID = new Dictionary<IPAddress, string>();
            dostepny = new Dictionary<string,bool>();
            foreach (var i in mapa_ID_PunktKontaktu)
            {
                dostepny.Add(i.Key, false);
                IP_ID.Add(i.Value, i.Key); 
            }

            if(ustawienia.SSLWlaczone)
            {
                port = portSSL;
                centrala = new CentralaSSL(ID_IP, IP_ID , port ,ustawienia.Certyfikat) ;
            }
            else
            {
                port = portBezSSL; 
                centrala = new Centrala(ID_IP, IP_ID, port);
            }

            centrala.NowePolaczenieOdNas += centrala_NowePolaczenieOdNas;
            centrala.NowePolaczenieDoNas += centrala_NowePolaczenieDoNas;
            wiadomosciownia = new Wiadomosciownia(buforownia, centrala, new CzytanieSkonczone(czekajNaZapytanie));

            foreach (var i in mapa_ID_PunktKontaktu){ wiadomosciownia.DodajUzytkownika(i.Key); }           
        }
        
        public event NowaWiadomosc NowaWiadomosc{
            add {
                wiadomosciownia.NowaWiadomosc += value;
            }
            remove {
                wiadomosciownia.NowaWiadomosc -= value;
            }
        }

        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;

        public bool CzyDostepny(string idUzytkownika) {
            return dostepny[idUzytkownika];
        }

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <param name="punktKontaktu"></param>
        public void DodajKontakt(string idUzytkownika, IPAddress punktKontaktu)
        {
            IP_ID.Add(punktKontaktu, idUzytkownika);
            ID_IP.Add(idUzytkownika, punktKontaktu);
            wiadomosciownia.DodajUzytkownika(idUzytkownika);
            dostepny.Add(idUzytkownika, false);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void UsunKontakt(string idUzytkownika)
        {
            IP_ID.Remove(ID_IP[idUzytkownika]);
            ID_IP.Remove(idUzytkownika);
            wiadomosciownia.UsunUzytkownika(idUzytkownika);
            buforownia.Usun(idUzytkownika);
            dostepny.Remove(idUzytkownika);
        }

        /// <summary>
        /// Polacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <returns></returns>
        public void ZainicjujPolaczenie(string idUzytkownika) {            
            var polaczenie = centrala[idUzytkownika];
            if (polaczenie == null) {
                centrala.NawiazPolaczenie(idUzytkownika);
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
            wiadomosciownia.WyslijWiadomosc(idRozmowcy, Protokol.ZwyklaWiadomosc , wiadomosc);
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
                    if (adresKlienta != null) { zajmijSieKlientem(adresKlienta); }                    
                }
            }
            catch (Exception ex) { Trace.TraceInformation("[Start]" + ex.ToString()); } // program zostal zamkniety
            finally { Stop(); }
        }

        /// <summary>
        /// zatrzymaj serwer
        /// </summary>
        public void Stop() {
            // zatrzymaj nasluch
            if (serwer != null) { serwer.Stop(); }
            centrala.RozlaczWszystkich();
        }

        public void OglosOpis() {
            ID_IP.Keys.ToList().ForEach(s => wiadomosciownia.WyslijWiadomosc(s, Protokol.OtoMojOpis , Opis));
        }

        public void PoprosOpis(string id)
        {
            wiadomosciownia.WyslijWiadomosc(id, Protokol.DajMiSwojOpis, "");
        }

        IPAddress dajIp(string idUzytkownika) {
            return ID_IP[idUzytkownika];
        }

        void centrala_NowePolaczenieDoNas(string idUzytkownika)
        {
            obsluzZmianaStanuPolaczenia(idUzytkownika, true);
        }

        void centrala_NowePolaczenieOdNas(string idUzytkownika)
        {
            obsluzZmianaStanuPolaczenia(idUzytkownika, true);
            czekajNaZapytanie(idUzytkownika);
        }

        void obsluzZmianaStanuPolaczenia(string idUzytkownika, bool nowyStan) 
        {
            dostepny[idUzytkownika] = nowyStan;
            if (ZmianaStanuPolaczenia != null)
            { ZmianaStanuPolaczenia(idUzytkownika); }
        }
        
        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="idNadawcy">Identyfikator nadawcy</param>
        void czekajNaZapytanie(string idNadawcy)
        {
            Trace.TraceInformation("Czekamy na zapytanie ");
            var wynik = new StatusObsluzZapytanie(){ idNadawcy=idNadawcy, rodzaj=new byte[1]};
            if (centrala[dajIp(idNadawcy)] == null) {
                Trace.TraceInformation("[czekajNaZapytanie] brak polaczenia");
                return;
            }
            centrala[dajIp(idNadawcy)].BeginRead(wynik.rodzaj, 0, 1, obsluzZapytanie, wynik);
        }

        void obsluzZapytanie(IAsyncResult wynik){
            var status = (StatusObsluzZapytanie)wynik.AsyncState;
            if (centrala[dajIp(status.idNadawcy)] == null) { return; }
            Trace.TraceInformation("Przyszlo nowe zapytanie: " + status.rodzaj[0].ToString());
            
            try {  centrala[dajIp(status.idNadawcy)].EndRead(wynik); }
            catch(Exception ex)
            {   // zostalismy rozlaczeni
                obsluzZmianaStanuPolaczenia(status.idNadawcy, false);
                return; 
            } 

            switch (status.rodzaj[0]) { 
                case Protokol.KoniecPolaczenia:
                    obsluzZmianaStanuPolaczenia(status.idNadawcy, false);
                    Rozlacz(status.idNadawcy);
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, TypWiadomosci.Zwykla);
                    break;
                case Protokol.DajMiSwojOpis: // prosza nas o nasz opis
                    czekajNaZapytanie(status.idNadawcy);
                    wiadomosciownia.WyslijWiadomosc(status.idNadawcy, Protokol.OtoMojOpis, Opis);
                    break;
                case Protokol.OtoMojOpis: // my prosimy o opis
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, TypWiadomosci.Opis);
                    break;
                default: // blad, czekaj na kolejne zapytanie
                    czekajNaZapytanie(status.idNadawcy);
                    break;
            }          
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
            czekajNaZapytanie(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        }

        class StatusObsluzZapytanie
        {
            public byte[] rodzaj;
            public String idNadawcy;
        }

    }
}
