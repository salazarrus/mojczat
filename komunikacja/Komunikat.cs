using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MojCzat.komunikacja
{
    class Komunikat
    {
        public const byte KoniecPolaczenia = 0;
        public const byte ZwyklaWiadomosc = 1;
        public const byte DajOpis = 2;
        public const byte WezOpis = 3;
        public const byte WezPlik = 4;
        public const byte DajPlik = 5;


        const int DlugoscNaglowka = 5; // 1 bajt na rodzaj komunikatu, 4 na dlugosc
        // stworz komunikat
        public static byte[] Generuj(byte rodzaj, string wiadomosc)
        {
            var dlugoscZawartosci = Encoding.UTF8.GetByteCount(wiadomosc);
            var bajtyZawartosc = Encoding.UTF8.GetBytes(wiadomosc);
            var bajty = new byte[dlugoscZawartosci + DlugoscNaglowka];
            var dlugoscZawartosciNaglowek = BitConverter.GetBytes(dlugoscZawartosci);
            if (BitConverter.IsLittleEndian) { dlugoscZawartosciNaglowek.Reverse(); }
            bajty[0] = rodzaj;
            Array.Copy(dlugoscZawartosciNaglowek, 0, bajty, 1, dlugoscZawartosciNaglowek.Length);
            Array.Copy(bajtyZawartosc, 0, bajty, DlugoscNaglowka, bajtyZawartosc.Length);
            return bajty;
        }    
    }
}
