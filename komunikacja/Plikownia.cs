#define TRACE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MojCzat.komunikacja
{
    /// <summary>
    /// Obiekt odpowiedzialny za przesylanie do i odbieranie od innych uzytkownikow plikow
    /// </summary>
    class Plikownia
    {
        Buforownia buforownia = new Buforownia(4096);

        public event PlikWyslano PlikWyslano;
        public event PlikZaoferowano PlikZaoferowano;

        public void WczytajNazwe(Stream strumien, String idUzytkownika ,int dlugoscNazwy) 
        {
            wczytajNazwe(strumien, idUzytkownika, dlugoscNazwy, 0);
        }

        void wczytajNazwe(Stream strumien, String idUzytkownika, int dlugoscNazwy, int wczytano)
        {
            strumien.BeginRead(buforownia[idUzytkownika], 0, dlugoscNazwy, nazwePlikuWczytano,
                new WczytajNazweStatus() { dlugosc = dlugoscNazwy, idUzytkownika = idUzytkownika, strumien = strumien });
        }


        void nazwePlikuWczytano(IAsyncResult wynik)
        {
            var status = (WczytajNazweStatus)wynik.AsyncState;
            
            int bajtyWczytane = status.strumien.EndRead(wynik);
            if (status.dlugosc > status.wczytano + bajtyWczytane)
            {
                wczytajNazwe(status.strumien, status.idUzytkownika, status.dlugosc ,status.wczytano + bajtyWczytane);
                return;
            }

            string nazwa = Encoding.UTF8.GetString(buforownia[status.idUzytkownika], 0, status.dlugosc);

            // czyscimy bufor
            Array.Clear(buforownia[status.idUzytkownika], 0, status.dlugosc);
            if (PlikZaoferowano != null) { PlikZaoferowano(status.idUzytkownika, nazwa); }

        }
        public void OferujPlik(string idUzytkownika, string plik, Stream strumien)
        {
            FileInfo f = new FileInfo(plik);
            
            Trace.TraceInformation("oferujemy plik  " + plik + " dla " + idUzytkownika);
            var komunikat = Komunikat.Generuj(Komunikat.WezPlik, f.Name);
            strumien.BeginWrite(komunikat, 0, komunikat.Length, zaoferowano, new
                OferujPlikStatus() { plik = plik, idUzytkownika = idUzytkownika, strumien = strumien });
        }




        void zaoferowano(IAsyncResult wynik) 
        {
            var status = (OferujPlikStatus)wynik.AsyncState;
            status.strumien.EndWrite(wynik);
            Trace.TraceInformation("wyslano oferte");
        }
        public void Wyslij(String idRozmowcy, String sciezka)
        {
            if(!File.Exists(sciezka)){ return; }
            var rozmiar = new FileInfo(sciezka).Length;
            //var strumienWyjscia = centrala[idRozmowcy];
        }

        class OferujPlikStatus
        { 
            public string idUzytkownika { get; set;}
            public string plik { get; set; }
            public Stream strumien { get; set; }
        }

        class WczytajNazweStatus
        {
            public string idUzytkownika { get; set; }
            public int dlugosc{ get; set; }
            public int wczytano{ get; set; }
            public Stream strumien { get; set; }
        
        }
    }
}
