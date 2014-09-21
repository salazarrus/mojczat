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
    public delegate void GotowyDoOdbioru(string guid);

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
        {
            centrala.ToNieDziala(idStrumienia);
        }

        /// <summary>
        /// Polacz sie z uzytkownikiem
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika</param>
        public void Polacz(string idUzytkownika)
        {
            var idStrumienia = centrala.Polacz(mapownik[idUzytkownika]);
            if (idStrumienia == null) { return; }
            if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == idUzytkownika))
            { polaczeniaZasadnicze.Add(idStrumienia, new PolaczenieZasadnicze() { IdUzytkownika = idUzytkownika }); }
            else
            { polaczeniaPlikowe.Add(idStrumienia, new PolaczeniePlikowe() { IdUzytkownika = idUzytkownika }); }
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

        /// <summary>
        /// Daj polaczenie na wiadomosci do uzytkownika
        /// </summary>
        /// <param name="idStrumienia">Identyfikator strumienia</param>
        /// <returns></returns>
        public IPolaczenie DajPolaczenieZasadnicze(string idStrumienia)
        {
            return polaczeniaZasadnicze.ContainsKey(idStrumienia) ? 
                polaczeniaZasadnicze[idStrumienia] : null;
        }

        /// <summary>
        /// Czy istnieje taki strumien?
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool CzyZnasz(string guid) 
        {
            return polaczeniaZasadnicze.ContainsKey(guid);
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
            if (polaczenie is PolaczenieZasadnicze)
            { polaczeniaZasadnicze.Add(idPolaczenie, (PolaczenieZasadnicze)polaczenie); }
            else if (polaczenie is PolaczeniePlikowe)
            { polaczeniaPlikowe.Add(idPolaczenie, (PolaczeniePlikowe)polaczenie); }
        }
        
        // centrala informuje, ze otwarto nowe polaczenie
        void centrala_OtwartoPolaczenie(string idStrumienia, Stream strumien, IPAddress ip)
        {
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();

            Trace.TraceInformation("otwarto polaczenie");
            var polaczenie = DajPolaczenieZasadnicze(idStrumienia);

            if (polaczenie is PolaczenieZasadnicze)
            {
                polaczenie.Strumien = strumien;
                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }

            }
            else if (polaczenie is PolaczeniePlikowe) { polaczenie.Strumien = strumien; }
            else if (DajStrumienZasadniczy(idUzytkownika) == null)
            {
                dodajPolaczenie(idStrumienia, new PolaczenieZasadnicze() 
                    { IdUzytkownika = idUzytkownika, Strumien = strumien });

                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
            }
            else
            {
                dodajPolaczenie(idStrumienia, new PolaczeniePlikowe() 
                    { IdUzytkownika = idStrumienia, Strumien = strumien });
            }
            if (GotowyDoOdbioru != null) { GotowyDoOdbioru(idStrumienia); }
        }
        
        //centrala informuje o zamknieciu polaczenie
        void centrala_ZamknietoPolaczenie(string idPolaczenia)
        {
            if (DajPolaczenieZasadnicze(idPolaczenia) != null)
            {
                var polaczenie = DajPolaczenieZasadnicze(idPolaczenia);
                usunStrumien(idPolaczenia);
                rozlaczPolaczeniaPlikowe(polaczenie.IdUzytkownika);

                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(polaczenie.IdUzytkownika); }

            }
            if (dajPolaczeniePlikowe(idPolaczenia) != null) { usunStrumien(idPolaczenia); }
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
