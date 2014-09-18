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

    public delegate void done(string id);

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


        public String opis = "";

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
            foreach (var i in mapa_ID_PunktKontaktu)
            {
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

            centrala.NawiazalismyPolaczenie += centrala_NawiazalismyPolaczenie;
            wiadomosciownia = new Wiadomosciownia(buforownia, centrala, new done(czekajNaZapytanie));
            // generuj mape mapa_IP_ID
            this.IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in mapa_ID_PunktKontaktu)
            {
                IP_ID.Add(i.Value, i.Key);
                wiadomosciownia.DodajUzytkownika(i.Key);
            }           
        }

        void centrala_NawiazalismyPolaczenie(string idUzytkownika, bool polaczenieOtwarte)
        {
            czekajNaZapytanie(idUzytkownika);
        }
        
        public event NowaWiadomosc NowaWiadomosc{
            add {
                wiadomosciownia.NowaWiadomosc += value;
            }
            remove {
                wiadomosciownia.NowaWiadomosc -= value;
            }
        }

        public event ZmianaStanuPolaczenia ZmianaStanuPolaczeniaWydarzenie {
            add
            {
                centrala.ZmianaStanuPolaczenia += value;
            }
            remove
            {
                centrala.ZmianaStanuPolaczenia -= value;
            }
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
                var polaczenie = centrala[idUzytkownika];
                if (polaczenie == null) {
                    centrala.NawiazPolaczenie(idUzytkownika);
                }
                //wiadomosciownia.WyslijWiadomosc(idUzytkownika, Protokol.OtoMojOpis, opis);
                //poprosOpis(idUzytkownika);
                return true;
            }
            catch(Exception ex){
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        void centrala_ZmianaStanuPolaczenia(string idUzytkownika, bool polaczenieOtwarte)
        {
            if (polaczenieOtwarte) {

                czekajNaZapytanie(idUzytkownika);
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
                wiadomosciownia.WyslijWiadomosc(idRozmowcy, Protokol.ZwyklaWiadomosc , wiadomosc);
                return true;// TODO zmienic
            }
            catch(Exception ex) 
            {
                MessageBox.Show(ex.ToString());
                return false; 
            }           
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
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
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
            ID_IP.Keys.ToList().ForEach(s => wiadomosciownia.WyslijWiadomosc(s, Protokol.OtoMojOpis , opis));
        }

        public void poprosOpis(string id)
        {
            wiadomosciownia.WyslijWiadomosc(id, Protokol.DajMiSwojOpis, "");
        }

        IPAddress dajIp(string idUzytkownika) {
            return ID_IP[idUzytkownika];
        }     
   
        /// <summary>
        /// Czekaj (pasywnie) na wiadomosci
        /// </summary>
        /// <param name="polaczenie">kanal, ktorym przychodzi wiadomosc</param>
        /// <param name="idRozmowcy">Identyfikator nadawcy</param>
        void czekajNaZapytanie(string idRozmowcy)
        {
            //MessageBox.Show("Czekam na zapytanie");
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
            catch(Exception ex) { // zostalismy rozlaczeni
                MessageBox.Show(ex.ToString());
                return;
            }
            
            var rodzaj = Encoding.UTF8.GetString(status.typ);

            switch (status.typ[0]) { 
                case Protokol.KoniecPolaczenia:
                    Rozlacz(status.idNadawcy);
                    /*if (ZmianaStanuPolaczeniaWydarzenie != null) 
                    {
                        ZmianaStanuPolaczeniaWydarzenie(status.idNadawcy, false); 
                    }*/
                    return;
                case Protokol.ZwyklaWiadomosc://zwykla wiadomosc
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, Wiadomosc.Zwykla);
                    break;
                case Protokol.DajMiSwojOpis:
                    wiadomosciownia.WyslijWiadomosc(status.idNadawcy, Protokol.OtoMojOpis, opis);
                    break;
                case Protokol.OtoMojOpis:
                    wiadomosciownia.czytajWiadomosc(status.idNadawcy, Wiadomosc.Opis);
                    break;
                default:
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
            
            // powiadom zainteresowanych o nowym polaczeniu
            /*if (ZmianaStanuPolaczeniaWydarzenie != null)
            { ZmianaStanuPolaczeniaWydarzenie(nadawca, true); }
            */
            czekajNaZapytanie(nadawca);// czekaj (pasywnie) na wiadomosc z tego polaczenia
        }

        class StatusObsluzZapytanie
        {
            public byte[] typ;
            public String idNadawcy;
        }

    }
}
