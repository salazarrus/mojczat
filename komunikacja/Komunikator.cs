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
using System.Threading;

namespace MojCzat.komunikacja
{    
    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(String id, TypWiadomosci rodzaj , String wiadomosc);

    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika);

    // Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    public class Komunikator
    {
        public String Opis { get; set; }
        
        // Dostepnosc uzytkownika
        Dictionary<string, bool> dostepnosc = new Dictionary<string,bool>();

        // Obiekt odpowiedzialny za laczenie się z innymi uzytkownikami
        Centrala centrala;

        Nasluchiwacz nasluchiwacz;

        Pingacz pingacz;

        Protokol protokol;

        Mapownik mapownik;
        const int portBezSSL = 5080;
        const int portSSL = 5443;

        Thread watekKomunikator;

        /// <summary>
        /// Konstruktor komunikatora
        /// </summary>
        /// <param name="ipepNaId">Mapowanie punktu kontatku (adres IP,Port) do 
        /// Identyfikatora rozmowcy wszystkich kontaktow uzytkownika</param>
        public Komunikator(Dictionary<string, IPAddress> mapa_ID_PunktKontaktu, Ustawienia ustawienia)
        {
            //inicjalizacja i wypelnianie mapowan pochodnych
            mapownik = new Mapownik(mapa_ID_PunktKontaktu);

            foreach (var i in mapa_ID_PunktKontaktu)
            { dostepnosc.Add(i.Key, false); }

            int port;
            if(ustawienia.SSLWlaczone)
            {
                port = portSSL;
                centrala = new CentralaSSL(mapownik , port ,ustawienia.Certyfikat) ;
            }
            else
            {
                port = portBezSSL; 
                centrala = new Centrala(mapownik, port);
            }
            pingacz = new Pingacz(centrala, dostepnosc);

            nasluchiwacz = new Nasluchiwacz(port);
           
            centrala.NowePolaczenieOdNas += centrala_NowePolaczenieOdNas;
            centrala.NowePolaczenieDoNas += centrala_NowePolaczenieDoNas;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;

            protokol = new Protokol(centrala, mapownik, ustawienia);
                 
        }

        void centrala_ZamknietoPolaczenie(string idUzytkownika)
        { obsluzZmianaStanuPolaczenia(idUzytkownika, false); }
        
        public event NowaWiadomosc NowaWiadomosc{
            add { protokol.NowaWiadomosc += value; }
            remove { protokol.NowaWiadomosc -= value; }
        }

        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;
        
        // Oczekuj nadchodzacych polaczen
        public void Start()
        {
            watekKomunikator = new Thread(start);
            watekKomunikator.Start();
        }

        void start() 
        {
            nasluchiwacz.NowyKlient += nasluchiwacz_NowyKlient;
            pingacz.Start();
            nasluchiwacz.Start();
        }

        // zatrzymaj serwer
        public void Stop()
        {
            pingacz.Stop();
            nasluchiwacz.Stop();
            centrala.RozlaczWszystkich();
            watekKomunikator.Join();
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(String idRozmowcy, String wiadomosc)
        { protokol.WyslijWiadomosc(idRozmowcy, Protokol.ZwyklaWiadomosc, wiadomosc); }

        public void OglosOpis()
        {
            mapownik.WszystkieId.ForEach(s => protokol.WyslijWiadomosc(s, Protokol.OtoMojOpis, Opis));
        }

        public void PoprosOpis(string id)
        { protokol.WyslijWiadomosc(id, Protokol.DajMiSwojOpis, ""); }


        public bool CzyDostepny(string idUzytkownika) 
        { return dostepnosc[idUzytkownika]; }
        
        /// <summary>
        /// Rozlacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void Rozlacz(string idUzytkownika) 
        { centrala.ZamknijPolaczenie(mapownik[idUzytkownika]); }

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <param name="punktKontaktu"></param>
        public void DodajKontakt(string idUzytkownika, IPAddress punktKontaktu)
        {
            mapownik.Dodaj(idUzytkownika, punktKontaktu);
            protokol.DodajUzytkownika(idUzytkownika);
            dostepnosc.Add(idUzytkownika, false);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void UsunKontakt(string idUzytkownika)
        {
            mapownik.Usun(idUzytkownika);
            protokol.UsunUzytkownika(idUzytkownika);
            dostepnosc.Remove(idUzytkownika);
        }

        /// <summary>
        /// Nadeszlo polaczenie, obslugujemy je
        /// </summary>
        /// <param name="polaczenie"></param>
        void zajmijSieNadawca(IPAddress ipNadawcy)
        {
            var nadawca = mapownik[ipNadawcy];
            protokol.czekajNaZapytanie(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        } 

        void nasluchiwacz_NowyKlient(TcpClient polaczenie)
        {
            var ipKlienta = ((IPEndPoint)polaczenie.Client.RemoteEndPoint).Address;
            if (!mapownik.CzyZnasz(ipKlienta)) { DodajKontakt(ipKlienta.ToString(), ipKlienta); }

            var adresKlienta = centrala.ZajmijSiePolaczeniem(polaczenie);
            zajmijSieNadawca(adresKlienta);
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
            dostepnosc[idUzytkownika] = nowyStan;
            if (ZmianaStanuPolaczenia != null)
            { ZmianaStanuPolaczenia(idUzytkownika); }
        }               
    }
}
