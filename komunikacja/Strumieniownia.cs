using MojCzat.model;
using System;
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
        public string NawiazPolaczenieZasadnicze(string idUzytkownika)
        {
            var idPolaczenia = Guid.NewGuid().ToString();
            polaczeniaZasadnicze.Add(idPolaczenia, new PolaczenieZasadnicze() 
                { IdUzytkownika = idUzytkownika });
            centrala.Polacz(idPolaczenia, mapownik[idUzytkownika]);
            if (idPolaczenia == null) { return null; }
            if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == idUzytkownika))
            { polaczeniaZasadnicze.Add(idPolaczenia, new PolaczenieZasadnicze() { IdUzytkownika = idUzytkownika }); }
            return idPolaczenia;
        }

        public string NawiazPolaczeniePlikowe(string sciezkaPliku, string idUzytkownika) 
        {
            var idPolaczenia = Guid.NewGuid().ToString();
            polaczeniaPlikowe.Add(idPolaczenia, new PolaczeniePlikowe() { 
                IdUzytkownika = idUzytkownika, Plik = sciezkaPliku });
            centrala.Polacz(idPolaczenia, mapownik[idUzytkownika]);
            Trace.TraceInformation("Strumieniownia.NawiazPolaczeniePlikowe " + idPolaczenia);

            return idPolaczenia;
        }

        /// <summary>
        /// Zamknij strumien
        /// </summary>
        /// <param name="idStrumienia">Identyfikator strumienia</param>
        public void Rozlacz(string idStrumienia)
        { centrala.Rozlacz(idStrumienia);}

        /// <summary>
        /// Zamknij polaczenia do uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void RozlaczUzytkownika(string idUzytkownika)
        {
            polaczeniaZasadnicze.Where(k => k.Value.IdUzytkownika == idUzytkownika).ToList().ForEach(
                k => centrala.Rozlacz(k.Key));
        }

        public IPolaczenie DajPolaczenie(string idPolaczenia)
        {
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

        // Dodaj nowe polaczenie 
        void dodajPolaczenie(string idPolaczenie, IPolaczenie polaczenie)
        {
            Trace.TraceInformation("Strumieniownia.dodajPolaczenie " + idPolaczenie);
            if (polaczenie is PolaczenieZasadnicze)
            { polaczeniaZasadnicze.Add(idPolaczenie, (PolaczenieZasadnicze)polaczenie); }
            else if (polaczenie is PolaczeniePlikowe)
            { 
                polaczeniaPlikowe.Add(idPolaczenie, (PolaczeniePlikowe)polaczenie);           
            }
        }
        
        // centrala informuje, ze otwarto nowe polaczenie
        void centrala_OtwartoPolaczenie(string idPolaczenia, Stream strumien, IPAddress ip)
        {
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();

            
            var polaczenie = DajPolaczenie(idPolaczenia);
         
            if (polaczenie is PolaczenieZasadnicze) // z naszej strony
            {
                Trace.TraceInformation("Strumieniownia.centrala_OtwartoPolaczenie " + idPolaczenia + " nasze zasadnicze");
                polaczenie.Strumien = strumien;
                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }

            }
            else if (polaczenie is PolaczeniePlikowe) { // z naszej strony 
                polaczenie.Strumien = strumien;
                Trace.TraceInformation("Strumieniownia.centrala_OtwartoPolaczenie " + idPolaczenia + " nasze plikowe");
                if (NawiazalismyPolaczeniePlikowe != null)
                { NawiazalismyPolaczeniePlikowe(idPolaczenia); }
            }
            else if (DajStrumienZasadniczy(idUzytkownika) == null) // z cudzej strony
            {
                Trace.TraceInformation("Strumieniownia.centrala_OtwartoPolaczenie " + idPolaczenia + " cudze zasadnicze");
                dodajPolaczenie(idPolaczenia, new PolaczenieZasadnicze() 
                    { IdUzytkownika = idUzytkownika, Strumien = strumien });

                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
            }
            else // z cudziej strony
            {
                Trace.TraceInformation("Strumieniownia.centrala_OtwartoPolaczenie " + idPolaczenia + " cudze plikowe");
                dodajPolaczenie(idPolaczenia, new PolaczeniePlikowe() 
                    { IdUzytkownika = idPolaczenia, Strumien = strumien });
            }
            if (GotowyDoOdbioru != null) { GotowyDoOdbioru(idPolaczenia); }
        }
        
        //centrala informuje o zamknieciu polaczenie
        void centrala_ZamknietoPolaczenie(string idPolaczenia)
        {
            Trace.TraceInformation("Strumieniownia.centrala_ZamknietoPolaczenie " + idPolaczenia);
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
                .ForEach(p => centrala.Rozlacz(p.Key));
        }

        // usun strumien
        void usunStrumien(string idStrumienia)
        {
            if (polaczeniaZasadnicze.ContainsKey(idStrumienia)) { polaczeniaZasadnicze.Remove(idStrumienia); }
            if (polaczeniaPlikowe.ContainsKey(idStrumienia)) { polaczeniaPlikowe.Remove(idStrumienia); }
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
}
