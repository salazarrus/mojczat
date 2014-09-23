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

        public event PlikZaoferowano PlikZaoferowano;

        public event PlikOdebrano PlikOdebrano;

        public void WczytajNazwe(StrumienSieciowy strumien, String idUzytkownika, int dlugoscNazwy)
        {
            var status = new WczytajNazweStatus()
            {
                DlugoscNazwy = dlugoscNazwy,
                IdUzytkownika = idUzytkownika,
                StrumienSieciowy = strumien,
                WczytanoBajtow = 0
            };
            wczytajCzescNazwyPliku(status);
        }

        public void OferujPlik(string idUzytkownika, string plik, StrumienSieciowy strumien)
        {
            FileInfo info = new FileInfo(plik);
            var status = new OferujPlikStatus()
            {
                NazwaPliku = plik,
                IdUzytkownika = idUzytkownika,
                StrumienSieciowy = strumien
            };

            var komunikat = Komunikat.Generuj(Komunikat.ChceszPlik, info.Name);
            strumien.BeginWrite(komunikat, 0, komunikat.Length, zaoferowanoPlik, status);
        }

        public void PoprosPlik(StrumienSieciowy strumien)
        {
            var komunikat = Komunikat.Generuj(Komunikat.DajPlik, "");
            strumien.BeginWrite(komunikat, 0, komunikat.Length, poproszonoOPlik, strumien);
        }

        public void WyslijPlik(StrumienSieciowy strumien, string sciezka)
        {
            FileInfo fi = new FileInfo(sciezka);
            FileStream fs = new FileStream(sciezka, FileMode.Open, FileAccess.Read);
            var status = new TransmitujPlikStatus()
            {
                RozmiarPliku = fi.Length,
                StrumienSieciowy = strumien,
                WczytanoBajtow = 0,
                ZapisanoBajtow = 0,
                plik = fs
            };
            var naglowek = Komunikat.GenerujNaglowek(Komunikat.WezPlik, (int)fi.Length);
            strumien.BeginWrite(naglowek, 0, naglowek.Length, wyslanoNaglowek, status);
        }

        public void PobierzPlik(StrumienSieciowy strumien, string plik, int rozmiarPliku)
        {
            FileStream fs = new FileStream(plik, FileMode.Create, FileAccess.Write);
            var status = new TransmitujPlikStatus()
            {
                RozmiarPliku = rozmiarPliku,
                StrumienSieciowy = strumien,
                WczytanoBajtow = 0,
                ZapisanoBajtow = 0,
                plik = fs
            };
            pobierzCzescPliku(status);
        }

        void wyslanoNaglowek(IAsyncResult wynik)
        {
            var status = (TransmitujPlikStatus)wynik.AsyncState;
            status.StrumienSieciowy.EndWrite(wynik);
            wyslijCzescPliku(status);
        }

        void wyslijCzescPliku(TransmitujPlikStatus status)
        {
            status.plik.BeginRead(buforownia[status.StrumienSieciowy.ID], 0,
                buforownia.RozmiarBufora, wysylaniePlikuWczytanoZDysku, status);
        }

        void wysylaniePlikuWczytanoZDysku(IAsyncResult wynik)
        {
            var status = (TransmitujPlikStatus)wynik.AsyncState;
            var wczytaneBajty = status.plik.EndRead(wynik);
            status.WczytanoBajtow += wczytaneBajty;

            status.StrumienSieciowy.BeginWrite(buforownia[status.StrumienSieciowy.ID],
                0, wczytaneBajty, wysylaniePlikuCzescWyslano, status);
        }

        void wysylaniePlikuCzescWyslano(IAsyncResult wynik)
        {
            var status = (TransmitujPlikStatus)wynik.AsyncState;
            status.StrumienSieciowy.EndWrite(wynik);
            status.ZapisanoBajtow = status.WczytanoBajtow;
            if (status.ZapisanoBajtow < status.RozmiarPliku)
            {
                wyslijCzescPliku(status);
                return;
            }
            status.plik.Close();
        }

        void pobierzCzescPliku(TransmitujPlikStatus status)
        {
            status.StrumienSieciowy.BeginRead(buforownia[status.StrumienSieciowy.ID],
                0, buforownia.RozmiarBufora, pobieraniePlikuZapiszCzesc, status);
        }

        void pobieraniePlikuZapiszCzesc(IAsyncResult wynik)
        {
            var status = (TransmitujPlikStatus)wynik.AsyncState;
            var bajtyWczytane = status.StrumienSieciowy.EndRead(wynik);
            status.WczytanoBajtow += bajtyWczytane;
            status.plik.BeginWrite(buforownia[status.StrumienSieciowy.ID], 0, bajtyWczytane,
                pobieraniePlikuCzescZapisano, status);
        }

        void pobieraniePlikuCzescZapisano(IAsyncResult wynik)
        {
            var status = (TransmitujPlikStatus)wynik.AsyncState;
            status.plik.EndWrite(wynik);
            status.ZapisanoBajtow = status.WczytanoBajtow;
            if (status.ZapisanoBajtow < status.RozmiarPliku)
            {
                pobierzCzescPliku(status);
                return;
            }
            if (PlikOdebrano != null) { PlikOdebrano(status.StrumienSieciowy.ID); }

            status.plik.Close();
        }

        void wczytajCzescNazwyPliku(WczytajNazweStatus status)
        {
            status.StrumienSieciowy.BeginRead(buforownia[status.IdUzytkownika], status.WczytanoBajtow,
                status.DlugoscNazwy - status.WczytanoBajtow, wczytanoCzescNazwyPliku, status);
        }

        void wczytanoCzescNazwyPliku(IAsyncResult wynik)
        {
            var status = (WczytajNazweStatus)wynik.AsyncState;

            int bajtyWczytane = status.StrumienSieciowy.EndRead(wynik);
            status.WczytanoBajtow += bajtyWczytane;
            if (status.DlugoscNazwy > status.WczytanoBajtow)
            {
                wczytajCzescNazwyPliku(status);
                return;
            }

            string nazwa = Encoding.UTF8.GetString(buforownia[status.IdUzytkownika], 0, status.DlugoscNazwy);

            Array.Clear(buforownia[status.IdUzytkownika], 0, status.DlugoscNazwy);
            if (PlikZaoferowano != null) { PlikZaoferowano(status.IdUzytkownika, nazwa, status.StrumienSieciowy.ID); }
        }

        void poproszonoOPlik(IAsyncResult wynik)
        {
            var strumien = (StrumienSieciowy)wynik.AsyncState;
            strumien.EndWrite(wynik);
        }

        void zaoferowanoPlik(IAsyncResult wynik)
        {
            var status = (OferujPlikStatus)wynik.AsyncState;
            status.StrumienSieciowy.EndWrite(wynik);
        }

        class OferujPlikStatus
        {
            public string IdUzytkownika { get; set; }
            public string NazwaPliku { get; set; }
            public StrumienSieciowy StrumienSieciowy { get; set; }
        }

        class WczytajNazweStatus
        {
            public string IdUzytkownika { get; set; }
            public int DlugoscNazwy { get; set; }
            public int WczytanoBajtow { get; set; }
            public StrumienSieciowy StrumienSieciowy { get; set; }
        }

        class TransmitujPlikStatus
        {
            public long RozmiarPliku { get; set; }
            public StrumienSieciowy StrumienSieciowy { get; set; }
            public int WczytanoBajtow { get; set; }
            public int ZapisanoBajtow { get; set; }
            public FileStream plik { get; set; }
        }
    }
}
