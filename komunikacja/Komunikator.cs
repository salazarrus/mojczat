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

    public delegate void NowyKlient(TcpClient polaczenie);

    /// <summary>
    /// Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    /// </summary>
    public class Komunikator
    {
        public String Opis { get; set; }
        
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
        /// Obiekt odpowiedzialny za laczenie się z innymi uzytkownikami
        /// </summary>
        Centrala centrala;
                
        Buforownia buforownia = new Buforownia();

        Nasluchiwacz nasluchiwacz;

        Protokol protokol;

        const int portBezSSL = 5080;
        const int portSSL = 5443;

        /// <summary>
        /// Konstruktor komunikatora
        /// </summary>
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do 
        /// Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPAddress> mapa_ID_PunktKontaktu, Ustawienia ustawienia)
        {
            //inicjalizacja i wypelnianie mapowan pochodnych
            this.ID_IP = mapa_ID_PunktKontaktu;
            this.IP_ID = new Dictionary<IPAddress, string>();
            dostepny = new Dictionary<string,bool>();
            foreach (var i in mapa_ID_PunktKontaktu)
            {
                dostepny.Add(i.Key, false);
                IP_ID.Add(i.Value, i.Key); 
            }
            int port;
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

            nasluchiwacz = new Nasluchiwacz(port);
           
            centrala.NowePolaczenieOdNas += centrala_NowePolaczenieOdNas;
            centrala.NowePolaczenieDoNas += centrala_NowePolaczenieDoNas;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;

            protokol = new Protokol(centrala, buforownia , ID_IP, IP_ID);
                 
        }

        void centrala_ZamknietoPolaczenie(string idUzytkownika)
        {
            obsluzZmianaStanuPolaczenia(idUzytkownika, false);
        }
        
        public event NowaWiadomosc NowaWiadomosc{
            add {
                protokol.NowaWiadomosc += value;
            }
            remove {
                protokol.NowaWiadomosc -= value;
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
            protokol.DodajUzytkownika(idUzytkownika);
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
            protokol.UsunUzytkownika(idUzytkownika);
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
            protokol.WyslijWiadomosc(idRozmowcy, Protokol.ZwyklaWiadomosc , wiadomosc);
        }

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Start() 
        {
            nasluchiwacz.NowyKlient += nasluchiwacz_NowyKlient;
            nasluchiwacz.Start();
        }

        void nasluchiwacz_NowyKlient(TcpClient polaczenie)
        {
            var adresKlienta = centrala.ZajmijSiePolaczeniem(polaczenie);
            zajmijSieKlientem(adresKlienta);
        }

        /// <summary>
        /// zatrzymaj serwer
        /// </summary>
        public void Stop() {
            nasluchiwacz.Stop();
            centrala.RozlaczWszystkich();
        }

        public void OglosOpis() {
            ID_IP.Keys.ToList().ForEach(s => protokol.WyslijWiadomosc(s, Protokol.OtoMojOpis , Opis));
        }

        public void PoprosOpis(string id)
        {
            protokol.WyslijWiadomosc(id, Protokol.DajMiSwojOpis, "");
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
            protokol.czekajNaZapytanie(idUzytkownika);
        }

        void obsluzZmianaStanuPolaczenia(string idUzytkownika, bool nowyStan) 
        {
            dostepny[idUzytkownika] = nowyStan;
            if (ZmianaStanuPolaczenia != null)
            { ZmianaStanuPolaczenia(idUzytkownika); }
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
            protokol.czekajNaZapytanie(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        } 

    }
}
