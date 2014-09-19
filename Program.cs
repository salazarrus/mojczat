#define TRACE

using MojCzat.komunikacja;
using MojCzat.model;
using MojCzat.ui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            Trace.TraceInformation("");
            Trace.TraceInformation("NEW RUN");
            Trace.TraceInformation("");
            Process[] pname = Process.GetProcessesByName("mojczat");
            if (pname.Length > 1) 
            {
                MessageBox.Show("Aplikacja jest juz otwarta na tym komputerze.");
                return; 
            } 
            
            
            starujApplikacje();
        }

        /// <summary>
        /// Wczytaj konfiguracje i pokaz glowne okno programu
        /// </summary>
        static void starujApplikacje() {
            try
            {

                // zaladuj liste kontaktow uzytkownika
                var kontakty = Kontakt.WczytajListeKontaktow("kontakty.xml");
                // uruchom okno glowne programu w glowny watku programu
                var ustawienia = Ustawienia.Wczytaj("ustawienia.xml");
                Application.Run(new OknoGlowne(kontakty, ustawienia));
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }       
    }
}
