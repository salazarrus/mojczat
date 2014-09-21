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
using System.Diagnostics;

namespace MojCzat.komunikacja
{    
    // delegata definiujaca funkcje obslugujace zdarzenie NowaWiadomosc
    public delegate void NowaWiadomosc(string id, TypWiadomosci rodzaj , string wiadomosc);

    public delegate void PlikZaoferowano(string idUzytkownika, string nazwa);
    
    public delegate void PlikWyslano(string id, string nazwa);

    public delegate void PlikOdebrano(string id, string nazwa);
 
    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika);

    /// <summary>
    /// Obiekt koordynujacy wartwe transportu
    /// </summary>
    public class Komunikator
    {
        public String Opis { get; set; }
        
        // Dostepnosc uzytkownikow
        Dictionary<string, bool> dostepnosc = new Dictionary<string,bool>();
        
        // obiekt uzywany do regularnego sprawdzania dostepnosci innych uzytkownikow
        System.Timers.Timer pingacz;

        Protokol protokol;

        Mapownik mapownik;

        // oddzielny watek dla komunikatora
        System.Threading.Thread watekKomunikator;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mapa_ID_IP">mapowanie identyfikatora uzytkownika do jego adresu IP</param>
        /// <param name="ustawienia">obiekt ustawien programu</param>
        public Komunikator(Dictionary<string, IPAddress> mapa_ID_IP, Ustawienia ustawienia)
        {
         
            foreach (var i in mapa_ID_IP) { dostepnosc.Add(i.Key, false); }

            zainicjujPingacz();
    
            mapownik = new Mapownik(mapa_ID_IP);
            protokol = new Protokol(mapownik, ustawienia);                 
        }
        
        /// <summary>
        /// Otwrzymalismy nowa wiadomosc
        /// </summary>
        public event NowaWiadomosc NowaWiadomosc
        {
            add { protokol.NowaWiadomosc += value; }
            remove { protokol.NowaWiadomosc -= value; }
        }

        public event PlikZaoferowano PlikZaoferowano 
        {
            add { protokol.PlikZaoferowano += value; }
            remove { protokol.PlikZaoferowano -= value; }
        }

        /// <summary>
        /// Polaczenie z uzytkownikiem zostalo zamkniete badz otwarte
        /// </summary>
        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;
        
        /// <summary>
        /// Rozpocznij dzialanie komunikatora
        /// </summary>
        public void Start()
        {
            watekKomunikator = new System.Threading.Thread(start);
            watekKomunikator.Start();
        }
      
        /// <summary>
        /// Zatrzymaj dzialanie komunikatora
        /// </summary>
        public void Stop()
        {
            protokol.Stop();
            pingacz.Stop();
            watekKomunikator.Join();
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="wiadomosc">tresc wiadomosci</param>
        public void WyslijWiadomosc(String idUzytkownika, String wiadomosc)
        { protokol.WyslijWiadomosc(idUzytkownika, wiadomosc); }

        /// <summary>
        /// Wyslij plik do innego uzytkownkika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="sciezka">sciezka do pliku</param>
        public void WyslijPlik(String idUzytkownika, String sciezka)
        { protokol.WyslijPlik(idUzytkownika, sciezka); }

        /// <summary>
        /// Wyslij swoj opis do uzytkownikow z listy kontaktow
        /// </summary>
        public void OglosOpis()
        { mapownik.WszystkieId.ForEach(s => 
            protokol.WyslijOpis(s, Opis)); }

        /// <summary>
        /// Popros o opis uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void PoprosOpis(string idUzytkownika)
        { protokol.PoprosOpis(idUzytkownika); }

        /// <summary>
        /// Sprawdz czy uzytkownik jest dostepny
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <returns></returns>
        public bool CzyDostepny(string idUzytkownika) 
        { return dostepnosc.ContainsKey(idUzytkownika) && dostepnosc[idUzytkownika]; }
        
        /// <summary>
        /// Rozlacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void Rozlacz(string idUzytkownika) 
        { protokol.Rozlacz(idUzytkownika); }

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        /// <param name="ip">adres IP</param>
        public void DodajKontakt(string idUzytkownika, IPAddress ip)
        {
            dodajKontakt(idUzytkownika, ip, false);
        }

        /// <summary>
        /// Usunieto uzytkownika z list kontaktow
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void UsunKontakt(string idUzytkownika)
        {           
            protokol.UsunUzytkownika(idUzytkownika);
            dostepnosc.Remove(idUzytkownika);
            mapownik.Usun(idUzytkownika);
        }

        // obluguj nowego uzytkownika
        void dodajKontakt(string idUzytkownika, IPAddress ip, bool dostepny)
        {
            if (mapownik.CzyZnasz(idUzytkownika)) { return; }
            mapownik.Dodaj(idUzytkownika, ip);
            protokol.DodajUzytkownika(idUzytkownika);
            dostepnosc.Add(idUzytkownika, dostepny);
        }
        
        // zainicjuj obiekt sprawdzajacy dostepnosc innych uzytkownikow
        void zainicjujPingacz()
        {
            this.pingacz = new System.Timers.Timer();
            pingacz.Elapsed += pingacz_Elapsed;
            pingacz.Interval = 6000;
        }

        // sproboj polaczyc sie z niedostepnymi uzytkownikami
        void sprobojPolaczyc() 
        {
            dostepnosc.Keys.Where(id => !dostepnosc[id]).ToList()
                .ForEach(id => protokol.Polacz(id));
        }

        void pingacz_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        { sprobojPolaczyc(); }

        // start komunikatora
        void start()
        {
            protokol.OtwartoPolaczenieZasadnicze += protokol_OtwartoPolaczenieZasadnicze;
            protokol.ZamknietoPolaczenieZasadnicze += protokol_ZamknietoPolaczenieZasadnicze;
            sprobojPolaczyc();
            pingacz.Start();
            protokol.Start();
        }

        // polaczenie do uzytkownika zostalo zamkniete
        void protokol_ZamknietoPolaczenieZasadnicze(string idUzytkownika)
        {
            dostepnosc[idUzytkownika] = false; 
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(idUzytkownika); }
        }

        // polaczenie do uzytkownika zostalo otwarte
        void protokol_OtwartoPolaczenieZasadnicze(string idUzytkownika)
        {
            if (!mapownik.CzyZnasz(idUzytkownika)) { 
                dodajKontakt(idUzytkownika, IPAddress.Parse(idUzytkownika), true);                
            }
            dostepnosc[idUzytkownika] = true;
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(idUzytkownika); }
        }

    }
}
