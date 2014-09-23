using MojCzat.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    public delegate void GotowyDoOdbioru(string idStrumienia);

    /// <summary>
    /// Obiekt odpowiedzialny za zarzadzanie strumieniami do innych uzytkownikow
    /// </summary>
    class Strumieniownia
    {
        Mapownik mapownik;
        Centrala centrala;

        // polaczenia wykorzystywane do przesylania wiadomosci tekstowych
        Dictionary<string, PolaczenieZasadnicze> polaczeniaZasadnicze = new Dictionary<string, PolaczenieZasadnicze>();
        // polaczenia do wysylania plikow
        Dictionary<string, PolaczeniePlikowe> polaczeniaPlikowe = new Dictionary<string, PolaczeniePlikowe>();

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="mapownik"></param>
        /// <param name="ustawienia"></param>
        public Strumieniownia(Mapownik mapownik, Ustawienia ustawienia)
        {
            this.mapownik = mapownik;
            this.centrala = ustawienia.SSLWlaczone ? new CentralaSSL(ustawienia.Certyfikat) : new Centrala();

            centrala.OtwartoPolaczenie += centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;
        }

        /// <summary>
        /// Polaczono sie z dotychczas niedostepnym uzytkownikiem
        /// </summary>
        public event OtwartoPolaczenieZasadnicze OtwartoPolaczenieZasadnicze;

        /// <summary>
        /// Wszystkie polaczenia do uzytkownika zostaly zamkniete
        /// </summary>
        public event ZamknietoPolaczenieZasadnicze ZamknietoPolaczenieZasadnicze;

        public event NawiazalismyPolaczeniePlikowe NawiazalismyPolaczeniePlikowe;

        /// <summary>
        /// Strumien jest gotowy do czytania z niego
        /// </summary>
        public event GotowyDoOdbioru GotowyDoOdbioru;

        /// <summary>
        /// Ropocznij dzialanie
        /// </summary>
        public void Start()
        { centrala.Start(); }

        /// <summary>
        /// Zakoncz dzialanie
        /// </summary>
        public void Stop()
        {
            centrala.OtwartoPolaczenie -= centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie -= centrala_ZamknietoPolaczenie;
            centrala.Stop();
        }

        /// <summary>
        /// Zglos, ze dane polaczenie nie dziala
        /// </summary>
        /// <param name="idStrumienia">Identyfikator strumienia</param>
        public void ToPolaczenieNieDziala(string idStrumienia)
        { centrala.ToNieDziala(idStrumienia); }

        /// <summary>
        /// Polacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void NawiazPolaczenieZasadnicze(string idUzytkownika)
        {
            var idPolaczenia = Guid.NewGuid().ToString();
            centrala.Polacz(idPolaczenia, mapownik[idUzytkownika]);
            return;
        }

        public string NawiazPolaczeniePlikowe(string sciezkaPliku, string idUzytkownika) 
        {
            var idPolaczenia = Guid.NewGuid().ToString();
            polaczeniaPlikowe.Add(idPolaczenia, new PolaczeniePlikowe()
            { 
                IdUzytkownika = idUzytkownika, Plik = sciezkaPliku });
            centrala.Polacz(idPolaczenia, mapownik[idUzytkownika]);

            return idPolaczenia;
        }

        /// <summary>
        /// Zamknij strumien
        /// </summary>
        /// <param name="idStrumienia">Identyfikator strumienia</param>
        public void Rozlacz(string idStrumienia)
        { centrala.Rozlacz(idStrumienia, "wiele");}

        /// <summary>
        /// Zamknij polaczenia do uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void RozlaczUzytkownika(string idUzytkownika)
        {
            polaczeniaZasadnicze.Where(k => k.Value.IdUzytkownika == idUzytkownika).ToList().ForEach(
                k => centrala.Rozlacz(k.Key, "uzytkownika"));
        }

        public IPolaczenie DajPolaczenie(string idPolaczenia)
        {
            Trace.TraceInformation("Strumieniownia.DajPolaczenie " + idPolaczenia);
            if(polaczeniaZasadnicze.ContainsKey(idPolaczenia))
            { return polaczeniaZasadnicze[idPolaczenia]; }
            
            if (polaczeniaPlikowe.ContainsKey(idPolaczenia))
            { return polaczeniaPlikowe[idPolaczenia]; }
         
            return null;
        }

        /// <summary>
        /// Czy istnieje taki strumien?
        /// </summary>
        /// <param name="idPolaczenia"></param>
        /// <returns></returns>
        public bool CzyZnasz(string idPolaczenia) 
        {
            return polaczeniaZasadnicze.ContainsKey(idPolaczenia)
                || polaczeniaPlikowe.ContainsKey(idPolaczenia);
        }

        /// <summary>
        /// Daj strumien na wiadomosci do uzytkownika
        /// </summary>
        /// <param name="idUzytkownika"></param>
        /// <returns></returns>
        public Stream DajStrumienZasadniczy(string idUzytkownika)
        {
            return polaczeniaZasadnicze.Values.Where(p => p.IdUzytkownika == idUzytkownika).
                Select(p => p.Strumien).SingleOrDefault();
        }
                
        // centrala informuje, ze otwarto nowe polaczenie
        void centrala_OtwartoPolaczenie(string idPolaczenia, Kierunek kierunek , Stream strumien, IPAddress ip)
        {
            Trace.TraceInformation("centrala_OtwartoPolaczenie " + idPolaczenia);
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();

            bool dodanoZasadnicze = false;
            lock (polaczeniaZasadnicze)
            {
                var mamyZasadnicze = polaczeniaZasadnicze.Any(p => p.Value.IdUzytkownika == idUzytkownika);
                if (!mamyZasadnicze)
                {
                    Trace.TraceInformation("centrala_OtwartoPolaczenie dodajemy zasadnicze " + (kierunek==Kierunek.OD_NAS?"od nas": "do nas"));
                    polaczeniaZasadnicze.Add(idPolaczenia, new PolaczenieZasadnicze() { IdUzytkownika = idUzytkownika, Strumien = strumien });
                    dodanoZasadnicze = true;
                }
            }
            if (dodanoZasadnicze && OtwartoPolaczenieZasadnicze != null) { OtwartoPolaczenieZasadnicze(idUzytkownika); }

            if (!dodanoZasadnicze)
            {
                if (kierunek == Kierunek.DO_NAS)
                {
                    Trace.TraceInformation("centrala_OtwartoPolaczenie dodajemy plikowe do nas");
                    polaczeniaPlikowe.Add(idPolaczenia, new PolaczeniePlikowe() { IdUzytkownika = idUzytkownika, Strumien = strumien });
                }
                else if (kierunek == Kierunek.OD_NAS)
                {
                    Trace.TraceInformation("centrala_OtwartoPolaczenie otwarto plikowe od nas");
                    if (!polaczeniaPlikowe.ContainsKey(idPolaczenia)) 
                    {
                        Rozlacz(idPolaczenia);
                    }
                    var polaczeniePlikowe = polaczeniaPlikowe[idPolaczenia];
                    polaczeniePlikowe.Strumien = strumien;
                    if (NawiazalismyPolaczeniePlikowe != null) { NawiazalismyPolaczeniePlikowe(idPolaczenia); }
                }
            }
            if (GotowyDoOdbioru != null) { GotowyDoOdbioru(idPolaczenia); }
        }
        
        //centrala informuje o zamknieciu polaczenie
        void centrala_ZamknietoPolaczenie(string idPolaczenia)
        {
            var polaczenie = DajPolaczenie(idPolaczenia);
            if (polaczenie is PolaczenieZasadnicze)
            {
                usunStrumien(idPolaczenia);
                rozlaczPolaczeniaPlikowe(polaczenie.IdUzytkownika);

                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(polaczenie.IdUzytkownika); }

            }
            if (polaczenie is PolaczeniePlikowe) { usunStrumien(idPolaczenia); }
        }

        // rozlacz polaczenia do przesylu plikow przez/do uzytkownika
        void rozlaczPolaczeniaPlikowe(string idUzytkownika)
        {
            polaczeniaPlikowe.Where(p => p.Value.IdUzytkownika == idUzytkownika).ToList()
                .ForEach(p => centrala.Rozlacz(p.Key, "plikowe"));
        }

        // usun strumien
        void usunStrumien(string idStrumienia)
        {
            Trace.TraceInformation("Strumieniownia.usunStrumien " + idStrumienia);

            if (polaczeniaZasadnicze.ContainsKey(idStrumienia)) 
            {
                polaczeniaZasadnicze.Remove(idStrumienia); 
            }
            if (polaczeniaPlikowe.ContainsKey(idStrumienia)) 
            {
                polaczeniaPlikowe.Remove(idStrumienia); 
            }
            Trace.TraceInformation("Strumieniownia.usunStrumien liczba zasadniczych" + polaczeniaZasadnicze.Count);
            
        }

        // znajdz polaczenie plikowe
        IPolaczenie dajPolaczeniePlikowe(string idUzytkownika)
        {
            return polaczeniaPlikowe.ContainsKey(idUzytkownika) ?
                polaczeniaPlikowe[idUzytkownika] : null;
        }        
    }

    interface IPolaczenie 
    {
        Stream Strumien { get; set; }
        string IdUzytkownika { get; set; }
    }

    class PolaczenieZasadnicze: IPolaczenie
    {
        public Stream Strumien { get; set; }
        public string IdUzytkownika { get; set; }
    }

    class PolaczeniePlikowe: IPolaczenie
    {
        public Stream Strumien { get; set; }
        public string IdUzytkownika { get; set; }
        public string Plik { get; set; }
    }

    public enum Kierunek
    { 
        OD_NAS,
        DO_NAS
    }


}
