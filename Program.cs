using MojCzat.komunikacja;
using MojCzat.model;
using MojCzat.ui;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace MojCzat
{
    /// <summary>
    /// Klasa glowna aplikacji
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Medota poczatkowa aplikacji
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // zaladuj liste kontaktow uzytkownika
            var kontakty = Kontakt.WczytajListeKontaktow("kontakty.xml");
            // stworz mape (adres IP,Port)->(ID) uzytkownikow z wczytanej listy kontakow
            var idNaIpep = new Dictionary<string, IPEndPoint>();
            kontakty.ForEach(k => idNaIpep.Add(k.ID, k.PunktKontaktu)); 
            // zainicjalizuj obiekt odpowiedzialny za przesylanie / odbieranie wiadomosci
            var komunikator = new Komunikator(idNaIpep);
            // uruchom oddzielny watek dla obiektu odpowiedzialnego za komunikacje
            var watekKomunikator = new Thread(komunikator.Sluchaj);
            watekKomunikator.Start();
            
            // uruchom okno glowne programu w glowny watku programu
            Application.Run(new OknoGlowne(kontakty, komunikator));
            
            // glowne okno programu zostalo zamkniete, dlatego zatrzymujemy dzialanie
            // obiektu odpowiedzialnego za komunikacje
            komunikator.Stop();
            // zakoncz watek obiektu odpowiedzialnego za komunikacje
            watekKomunikator.Join();
        }
    }
}
