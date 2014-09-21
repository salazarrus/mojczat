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
        Dictionary<string, PolaczenieZasadnicze> polaczeniaZasadnicze = new Dictionary<string, PolaczenieZasadnicze>();
        Dictionary<string, PolaczeniePlikowe> polaczeniaPlikowe = new Dictionary<string, PolaczeniePlikowe>();


        public Strumieniownia(Mapownik mapownik, Ustawienia ustawienia)
        {
            this.mapownik = mapownik;
            this.centrala = ustawienia.SSLWlaczone ? new CentralaSSL(ustawienia.Certyfikat) : new Centrala();

            centrala.OtwartoPolaczenie += centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie += centrala_ZamknietoPolaczenie;
        }

        public event OtwartoPolaczenieZasadnicze OtwartoPolaczenieZasadnicze;

        public event ZamknietoPolaczenieZasadnicze ZamknietoPolaczenieZasadnicze;

        public event GotowyDoOdbioru GotowyDoOdbioru;

        public void Start()
        { centrala.Start(); }

        public void Stop()
        {
            centrala.OtwartoPolaczenie -= centrala_OtwartoPolaczenie;
            centrala.ZamknietoPolaczenie -= centrala_ZamknietoPolaczenie;
            centrala.Stop();
        }

        public void ToNieDziala(string guidStrumienia)
        {
            centrala.ToNieDziala(guidStrumienia);
        }

        public void Polacz(string id)
        {
            var guid = centrala.Polacz(mapownik[id]);
            if (guid == null) { return; }
            if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == id))
            { polaczeniaZasadnicze.Add(guid, new PolaczenieZasadnicze() { IdUzytkownika = id }); }
            else
            { polaczeniaPlikowe.Add(guid, new PolaczeniePlikowe() { IdUzytkownika = id }); }
        }
        public void Rozlacz(string guidStrumienia)
        {
            centrala.Rozlacz(guidStrumienia);
        }


        public void RozlaczUzytkownika(string id)
        {
            polaczeniaZasadnicze.Where(k => k.Value.IdUzytkownika == id).ToList().ForEach(
                k => centrala.Rozlacz(k.Key));
        }

        public IPolaczenie DajPolaczenieZasadnicze(string guid)
        {
            return polaczeniaZasadnicze.ContainsKey(guid) ? 
                polaczeniaZasadnicze[guid] : null;
        }


        public bool CzyZnasz(string guid) 
        {
            return polaczeniaZasadnicze.ContainsKey(guid);
        }


        public Stream StrumienZasadniczy(string idUzytkownika)
        {
            return polaczeniaZasadnicze.Values.Where(p => p.IdUzytkownika == idUzytkownika).
                Select(p => p.Strumien).SingleOrDefault();
        }

        void DodajPolaczenie(string guid, IPolaczenie polaczenie)
        {
            if (polaczenie is PolaczenieZasadnicze)
            { polaczeniaZasadnicze.Add(guid, (PolaczenieZasadnicze)polaczenie); }
            else if (polaczenie is PolaczeniePlikowe)
            { polaczeniaPlikowe.Add(guid, (PolaczeniePlikowe)polaczenie); }
        }

        bool IstniejePolaczenieZasadnicze(string idUzytkownika)
        {
            return polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == idUzytkownika);
        }

        void centrala_OtwartoPolaczenie(string guid, Stream strumien, IPAddress ip)
        {
            string idUzytkownika;
            idUzytkownika = mapownik.CzyZnasz(ip) ? mapownik[ip] : ip.ToString();

            Trace.TraceInformation("otwarto polaczenie");
            var polaczenie = DajPolaczenieZasadnicze(guid);

            if (polaczenie is PolaczenieZasadnicze)
            {
                polaczenie.Strumien = strumien;
                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }

            }
            else if (polaczenie is PolaczeniePlikowe)
            {
                polaczenie.Strumien = strumien;
            }
            else if (!IstniejePolaczenieZasadnicze(idUzytkownika))
            {
                DodajPolaczenie(guid, new PolaczenieZasadnicze() { IdUzytkownika = idUzytkownika, Strumien = strumien });

                if (OtwartoPolaczenieZasadnicze != null)
                { OtwartoPolaczenieZasadnicze(idUzytkownika); }
            }
            else
            {
                DodajPolaczenie(guid, new PolaczeniePlikowe() { IdUzytkownika = guid, Strumien = strumien });
            }
            if (GotowyDoOdbioru != null) { GotowyDoOdbioru(guid); }
        }
        
        void centrala_ZamknietoPolaczenie(string guid)
        {
            if (DajPolaczenieZasadnicze(guid) != null)
            {
                var kanal = DajPolaczenieZasadnicze(guid);
                Usun(guid);
                RozlaczPolaczeniaPlikowe(kanal.IdUzytkownika);

                if (ZamknietoPolaczenieZasadnicze != null)
                { ZamknietoPolaczenieZasadnicze(kanal.IdUzytkownika); }

            }
            if (DajPolaczeniePlikowe(guid) != null) { Usun(guid); }
        }


        void RozlaczPolaczeniaPlikowe(string idUzytkownika)
        {
            polaczeniaPlikowe.Where(p => p.Value.IdUzytkownika == idUzytkownika).ToList()
                .ForEach(p => centrala.Rozlacz(p.Key));
        }

        void Usun(string guid)
        {
            if (polaczeniaZasadnicze.ContainsKey(guid)) { polaczeniaZasadnicze.Remove(guid); }
            if (polaczeniaPlikowe.ContainsKey(guid)) { polaczeniaPlikowe.Remove(guid); }
        }

        IPolaczenie DajPolaczeniePlikowe(string guid)
        {
            return polaczeniaPlikowe.ContainsKey(guid) ?
                polaczeniaPlikowe[guid] : null;
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
