﻿using MojCzat.komunikacja;
using System;
using System.Configuration;
using System.Windows.Forms;

namespace MojCzat.ui
{
    

    /// <summary>
    /// Okno rozmowy z innym uzytkownikiem
    /// </summary>
    public partial class OknoCzat : Form
    {
        /// <summary>
        /// Identyfikator drugiego uczestnika czatu
        /// </summary>
        string idRozmowcy;

        /// <summary>
        /// Identyfikator wlasny
        /// </summary>
        string mojeId;

        /// <summary>
        /// Obiekt odpowiedzialny za przesylanie i odbieranie wiadomosci
        /// </summary>
        Komunikator komunikator;

        /// <summary>
        /// obiekt do wyswietlania wiadomosci przez inny watek
        /// </summary>
        WyswietlNowaWiadomosc wyswietlWiadomoscDelegata;
        
        /// <summary>
        /// publiczny dostep do pola idRozmowcy
        /// </summary>
        public string IDRozmowcy
        {
            get { return idRozmowcy; }
        }

        /// <summary>
        /// Konstruktor okna czatu
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator drugiego uczestnika czatu</param>
        /// <param name="komunikator">Obiekt odpowiedzialny za przesylanie i odbieranie wiadomosci</param>
        public OknoCzat(string idRozmowcy, Komunikator komunikator)
        {           
            // inicjalizacja elementow graficznych okna
            InitializeComponent();
            // ustalanie naglowka okna
            this.Text = String.Format("Mój Czat z {0}", idRozmowcy);
            var p = Parent;
            this.mojeId = ConfigurationManager.AppSettings["mojeId"]; 
            // zapisywanie referencji
            this.idRozmowcy = idRozmowcy;
            this.komunikator = komunikator;

            this.wyswietlWiadomoscDelegata = new WyswietlNowaWiadomosc(WyswietlWiadomosc);
            // zapisywanie sie jako sluchacz wydarzenia NowaWiadomosc
        }

        /// <summary>
        /// Delegata dla wyswietlania wiadomosci przez inny watek
        /// </summary>
        /// <param name="wiadomosc"></param>
        delegate void WyswietlNowaWiadomosc(string wiadomosc);

        public void WyswietlWiadomosc(string wiadomosc)
        {
            tbCzat.AppendText(String.Format("[{0}] {1}\n", IDRozmowcy, wiadomosc));
        }

        /// <summary>
        /// Centralne pozycjonowanie okna wzledem rodzica
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);            
            CenterToParent();
        }

        /// <summary>
        /// Wiadomosc zostala wyslana, czyscimy wiec pole wpisywania wiadomosci
        /// </summary>
        void wyczyscPoleWiadomosci(){
            // wyczysc pole z tekstu
            tbWiadomosc.Text = String.Empty;
            // ustaw kursor w pozycji poczatkowej
            tbWiadomosc.Select(0, 0);
            // przygotuj do wpisywania
            tbWiadomosc.Focus();            
        }

        /// <summary>
        /// wysylamy nowa wiadomosc
        /// </summary>
        void wyslijWpisanaWiadomosc() {
            String wiadomosc = tbWiadomosc.Text;

            // usuwamy biale znaki z lewej i prawej strony tekstu
            wiadomosc = wiadomosc.Trim();
            // nie chcemy wysylac pustych wiadomosci
            if (wiadomosc == String.Empty) { return; }

            // wysylamy wiadomosc
            komunikator.Pisz(IDRozmowcy, wiadomosc);
                       
            // dodajemy wiadomosc do naszego okna czatu
            tbCzat.AppendText(String.Format("[{0}] {1}\n", mojeId, wiadomosc));
            // czyscimy pole wpisywania dla nowej wiadomosci
            wyczyscPoleWiadomosci();
        }

        // obługa zdarzeń interfejsu uzytkownika - poczatek

        // klikniecie przycisku "Wyslij"
        void btnWyslij_Click(object sender, EventArgs e)
        {
            wyslijWpisanaWiadomosc();
        }

        // Wcisniecie klawisza w polu wpisywania wiadomosci
        void tbWiadomosc_KeyDown(object sender, KeyEventArgs e)
        {
            // jesli wlaczona jest funkcja wysylania po wcisnieciu klawisza Enter i 
            // ten klawisz zostal wcisniety, wysylamy wiadomosc
            if (e.KeyCode == Keys.Enter && cbWyslijEnter.Checked) {
                wyslijWpisanaWiadomosc();
            }
        }

        // obługa zdarzeń interfejsu uzytkownika - koniec
    }
}