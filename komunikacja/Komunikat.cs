using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MojCzat.komunikacja
{
    class Komunikat
    {
        /// <summary>
        /// Polaczenie zostalo zamkniete
        /// </summary>
        public const byte KoniecPolaczenia = 0;
        /// <summary>
        /// Wiadomosc tekstowa
        /// </summary>
        public const byte ZwyklaWiadomosc = 1;
        /// <summary>
        /// Popros o opis uzytkownika
        /// </summary>
        public const byte DajOpis = 2;
        /// <summary>
        /// Odbierz opis uzytkownika
        /// </summary>
        public const byte WezOpis = 3;
        /// <summary>
        /// Zaoferuj plik uzytkownikowi
        /// </summary>
        public const byte ChceszPlik = 4;
        /// <summary>
        /// Popros o zaoferowany plik
        /// </summary>
        public const byte DajPlik = 5;
        /// <summary>
        /// Przyjmij poproszony plik
        /// </summary>
        public const byte WezPlik = 6;

        /// <summary>
        /// 1 bajt na rodzaj komunikatu, 4 na dlugosc komunikatu
        /// </summary>
        public const int DlugoscNaglowka = 5;

        /// <summary>
        /// stworz komunikat
        /// </summary>
        /// <param name="rodzaj">typ komunikatu</param>
        /// <param name="wiadomosc">tresc komunikatu</param>
        /// <returns></returns>
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

        /// <summary>
        /// Stworz naglowek komunikatu
        /// </summary>
        /// <param name="rodzaj">typ komunikatu</param>
        /// <param name="dlugosc">dlugosc komunikatu</param>
        /// <returns></returns>
        public static byte[] GenerujNaglowek(byte rodzaj, int dlugosc)
        {
            var bajty = new byte[DlugoscNaglowka];
            var dlugoscZawartosciNaglowek = BitConverter.GetBytes(dlugosc);
            if (BitConverter.IsLittleEndian) { dlugoscZawartosciNaglowek.Reverse(); }
            bajty[0] = rodzaj;
            Array.Copy(dlugoscZawartosciNaglowek, 0, bajty, 1, dlugoscZawartosciNaglowek.Length);
            return bajty;
        }
    }
}
