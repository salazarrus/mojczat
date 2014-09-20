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

    public delegate void PlikWyslano(string id, string nazwa); 

    // delegata definiujaca funkcje obslugujace zdarzenie ZmianaStanuPolaczenia
    public delegate void ZmianaStanuPolaczenia(string idUzytkownika);

    // Obiekt odpowiedzialny za odbieranie i przesylanie wiadomosci
    public class Komunikator
    {
        public String Opis { get; set; }
        
        // Dostepnosc uzytkownika
        Dictionary<string, bool> dostepnosc = new Dictionary<string,bool>();

        System.Timers.Timer timer;

        Protokol protokol;

        Mapownik mapownik;

        System.Threading.Thread watekKomunikator;

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

            zainicjujPingacz();

            protokol = new Protokol(mapownik, ustawienia);                 
        }
        
        public event NowaWiadomosc NowaWiadomosc{
            add { protokol.NowaWiadomosc += value; }
            remove { protokol.NowaWiadomosc -= value; }
        }

        public event ZmianaStanuPolaczenia ZmianaStanuPolaczenia;
        
        // Oczekuj nadchodzacych polaczen
        public void Start()
        {
            watekKomunikator = new System.Threading.Thread(start);
            watekKomunikator.Start();
        }
      
        // zatrzymaj serwer
        public void Stop()
        {
            protokol.Stop();
            timer.Stop();
            watekKomunikator.Join();
        }

        /// <summary>
        /// Wyslij wiadomosc tekstowa do innego uzytkownika
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Nowa wiadomosc</param>
        public void WyslijWiadomosc(String idRozmowcy, String wiadomosc)
        { protokol.WyslijWiadomosc(idRozmowcy, Protokol.ZwyklaWiadomosc, wiadomosc); }

        public void WyslijPlik(String idRozmowcy, String sciezka)
        { protokol.WyslijPlik(idRozmowcy, sciezka); }

        public void OglosOpis()
        { mapownik.WszystkieId.ForEach(s => 
            protokol.WyslijWiadomosc(s, Protokol.OtoMojOpis, Opis)); }

        public void PoprosOpis(string id)
        { protokol.WyslijWiadomosc(id, Protokol.DajMiSwojOpis, ""); }


        public bool CzyDostepny(string idUzytkownika) 
        { return dostepnosc[idUzytkownika]; }
        
        /// <summary>
        /// Rozlacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika"></param>
        public void Rozlacz(string idUzytkownika) 
        { protokol.Rozlacz(idUzytkownika); }

        /// <summary>
        /// Nowy uzytkownik na liscie kontaktow
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <param name="punktKontaktu"></param>
        public void DodajKontakt(string idUzytkownika, IPAddress ip)
        {
            if (mapownik.CzyZnasz(idUzytkownika)) { return; }
            mapownik.Dodaj(idUzytkownika, ip);
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

        void zainicjujPingacz()
        {
            this.timer = new System.Timers.Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Interval = 60000;
        }

        void sprobojPolaczyc() 
        {
            dostepnosc.Keys.Where(id => !dostepnosc[id]).ToList()
                .ForEach(id => protokol.Polacz(id));
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        { sprobojPolaczyc(); }

        void start()
        {
            protokol.OtwartoPolaczenieZasadnicze += protokol_OtwartoPolaczenieZasadnicze;
            protokol.ZamknietoPolaczenieZasadnicze += protokol_ZamknietoPolaczenieZasadnicze;
            sprobojPolaczyc();
            timer.Start();
            protokol.Start();
        }

        void protokol_ZamknietoPolaczenieZasadnicze(string idUzytkownika)
        {
            dostepnosc[idUzytkownika] = false;
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(idUzytkownika); }
        }

        void protokol_OtwartoPolaczenieZasadnicze(string idUzytkownika)
        {
            // TODO a co jak nieznany?
            dostepnosc[idUzytkownika] = true;
            if (ZmianaStanuPolaczenia != null) { ZmianaStanuPolaczenia(idUzytkownika); }
        }

    }
}
