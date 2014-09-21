using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    class Strumieniownia
    {
        Mapownik mapownik;
        Centrala centrala;

        public Strumieniownia(Mapownik mapownik, Centrala centrala)
        {
            this.mapownik = mapownik;
            this.centrala = centrala;
        }

        Dictionary<string, PolaczenieZasadnicze> polaczeniaZasadnicze = new Dictionary<string, PolaczenieZasadnicze>();
        Dictionary<string, PolaczeniePlikowe> polaczeniaPlikowe = new Dictionary<string, PolaczeniePlikowe>();

        public void Polacz(string id)
        {
            var guid = centrala.Polacz(mapownik[id]);
            if (guid == null) { return; }
            if (!polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == id))
            { polaczeniaZasadnicze.Add(guid, new PolaczenieZasadnicze() { IdUzytkownika = id }); }
            else
            { polaczeniaPlikowe.Add(guid, new PolaczeniePlikowe() { IdUzytkownika = id }); }
        }

        public void RozlaczUzytkownika(string id)
        {
            polaczeniaZasadnicze.Where(k => k.Value.IdUzytkownika == id).ToList().ForEach(
                k => centrala.Rozlacz(k.Key));
        }

        public void RozlaczPolaczeniaPlikowe(string idUzytkownika)
        {
            polaczeniaPlikowe.Where(p => p.Value.IdUzytkownika == idUzytkownika).ToList()
                .ForEach(p => centrala.Rozlacz(p.Key));
        }

        public void Usun(string guid) 
        {
            if (polaczeniaZasadnicze.ContainsKey(guid)) { polaczeniaZasadnicze.Remove(guid); }
            if (polaczeniaPlikowe.ContainsKey(guid)) { polaczeniaPlikowe.Remove(guid); }
        }
      
        public IPolaczenie DajPolaczenieZasadnicze(string guid)
        {
            return polaczeniaZasadnicze.ContainsKey(guid) ? 
                polaczeniaZasadnicze[guid] : null;
        }

        public IPolaczenie DajPolaczeniePlikowe(string guid)
        {
            return polaczeniaPlikowe.ContainsKey(guid) ?
                polaczeniaPlikowe[guid]: null;
        }

        public bool CzyZnasz(string guid) 
        {
            return polaczeniaZasadnicze.ContainsKey(guid);
        }

        public void DodajPolaczenie(string guid, IPolaczenie polaczenie)
        {
            if (polaczenie is PolaczenieZasadnicze) 
            { polaczeniaZasadnicze.Add(guid, (PolaczenieZasadnicze)polaczenie); }
            else if(polaczenie is PolaczeniePlikowe)
            { polaczeniaPlikowe.Add(guid, (PolaczeniePlikowe)polaczenie); }
        }

        public bool IstniejePolaczenieZasadnicze(string idUzytkownika) 
        {
            return polaczeniaZasadnicze.Values.Any(p => p.IdUzytkownika == idUzytkownika);
        }

        public Stream StrumienZasadniczy(string idUzytkownika)
        {
            return polaczeniaZasadnicze.Values.Where(p => p.IdUzytkownika == idUzytkownika).
                Select(p => p.Strumien).SingleOrDefault();
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
