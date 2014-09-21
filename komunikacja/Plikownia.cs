using System;
using System.Collections.Generic;
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

        public Plikownia()
        { 
            
        }

        public event PlikWyslano PlikWyslano;


        public void Wyslij(String idRozmowcy, String sciezka)
        {
            if(!File.Exists(sciezka)){ return; }
            var rozmiar = new FileInfo(sciezka).Length;
            //var strumienWyjscia = centrala[idRozmowcy];


        }
    }
}
