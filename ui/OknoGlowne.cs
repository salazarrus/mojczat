﻿using MojCzat.komunikacja;
using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;

namespace MojCzat.ui
{
    /// <summary>
    /// W tym oknie znajduje sie lista kontaktow oraz ogole menu aplikacji
    /// </summary>
    public partial class OknoGlowne : Form
    {
        
        /// <summary>
        /// obiektu tego uzywamy do otwierania okna czatu z innego watku
        /// </summary>
        OtworzOknoCzatuZWiadomoscia otworzOknoCzatuDelegata;
        
        /// <summary>
        /// lista kontaktow uzytkownika
        /// </summary>
        List<Kontakt> kontakty;
        
        /// <summary>
        /// obiekt odpowiedzialny za odbieranie / przesylanie wiadomosci
        /// </summary>
        Komunikator komunikator;
        
        /// <summary>
        /// list otwartych okien czatu
        /// </summary>
        Dictionary<String, OknoCzat> otwarteOkna = new Dictionary<string, OknoCzat>(); 

        /// <summary>
        /// Konstruktor okna glownego
        /// </summary>
        /// <param name="listaKontaktow">elementy tej listy prezentowane sa w spisie kontaktow</param>
        /// <param name="komunikator">referencja do komunikatora potrzebna jest do zapisania sie jako sluchacz wydarzen (nowa wiadomosc etc.)</param>
        public OknoGlowne(List<Kontakt> listaKontaktow, Komunikator komunikator)
        {
            // inicjalizacja elementow formy
            InitializeComponent();
            
            // centralne ustawienie okna na pulpicie
            CenterToScreen();
            
            // ustalamy naglowek okna
            this.Text = String.Format("Mój Czat ({0})", ConfigurationManager.AppSettings["mojeId"]);
            
            // zachowujemy referencje
            this.kontakty = listaKontaktow;
            this.komunikator = komunikator;
            
            // zapisujemy sie jako sluchacz wydarzenia NowaWiadomosc
            komunikator.NowaWiadomosc += komunikator_NowaWiadomosc;

            // zaladuj elementy obiektu "kontakty" do interfejsu uzytkownika
            zaladujListeKontaktow();
          
            // inicjalizacja delegaty do otwierania okna czatu 
            otworzOknoCzatuDelegata = new OtworzOknoCzatuZWiadomoscia(otworzOknoCzat);
        }

        // ponizsza delegata jest konieczna do otwierania nowego okna czatu z watku Komunikatora
        delegate void OtworzOknoCzatuZWiadomoscia(string id, string wiadomosc);

        /// <summary>
        /// komunikator przekazal nam nowa wiadomosc
        /// </summary>
        /// <param name="id">Id nadawcy</param>
        /// <param name="wiadomosc">now wiadomosc</param>
         void komunikator_NowaWiadomosc(string id, string wiadomosc)
        {
            // otworz okno przez delegate poniewaz jestesmy w innym watku
            Invoke(otworzOknoCzatuDelegata, id, wiadomosc);
        }
        
        /// <summary>
        /// Podepnij liste kontaktow interfejsu uzytkownika
        /// </summary>
        void zaladujListeKontaktow(){
            // sortowanie - najpierw dostepni, potem kolejosc alfabetyczna
            var listaUI = kontakty.OrderByDescending(k=>k.Dostepny).ThenBy(k=>k.ID).ToList();
            
            lbKontakty.DataSource = listaUI;
        }

        void otworzOknoCzat(string idRozmowcy, string wiadomosc)
        {
            var okno = otworzOknoCzat(idRozmowcy);
            okno.WyswietlWiadomosc(wiadomosc);
        }
        /// <summary>
        /// Pokaz okno czatu z innym uzytkownikiem. Jesli okno jeszcze nie istnieje, stworz je
        /// </summary>
        /// <param name="idRozmowcy">Id rozmowcy</param>
        OknoCzat otworzOknoCzat(string idRozmowcy) {
            // jesli okno czatu jest juz otware, pokaz je
            if (otwarteOkna.ContainsKey(idRozmowcy))
            { 
                // znajdz okno
                OknoCzat otwarteOkno = otwarteOkna[idRozmowcy];
                // jesli okno jest zminimalizowane, przywroc je na pulpit
                if (otwarteOkno.WindowState == FormWindowState.Minimized) 
                {
                    otwarteOkno.WindowState = FormWindowState.Normal;
                }
                // pokaz okno na pierwszym planie
                otwarteOkno.BringToFront(); 
            }
            else //nie bylo otwarte, wiec otworz nowe
            {
                OknoCzat noweOkno = new OknoCzat(idRozmowcy, komunikator);
                // zachowujemy nowe okno na liscie otwartych okien
                otwarteOkna.Add(idRozmowcy, noweOkno);
                // pokazujemy nowe okno
                noweOkno.Show(this);
            }
            return otwarteOkna[idRozmowcy];
        }


        // obługa zdarzeń interfejsu uzytkownika - poczatek

        private void btnDodaj_Click(object sender, EventArgs e)
        {

        }

        private void btnUsun_Click(object sender, EventArgs e)
        {

        }

        private void btnWyszukaj_Click(object sender, EventArgs e)
        {

        }

        private void btnHistoria_Click(object sender, EventArgs e)
        {

        }

        private void btnUstawienia_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Dwukrotne klikniecie na liste kontaktow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbKontatky_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // nie trafilismy w element listy
            if (lbKontakty.IndexFromPoint(e.Location) == System.Windows.Forms.ListBox.NoMatches) { return; }
            if (lbKontakty.SelectedItem != null) 
            {
                var elementWybrany = (Kontakt)lbKontakty.SelectedItem;
                otworzOknoCzat(elementWybrany.ID); 
            }
        }

        /// <summary>
        /// Wcisniecie klawisza w polu listy kontaktow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbKontatky_KeyDown(object sender, KeyEventArgs e)
        {
            // reagujemy na klawisz Enter
            if (e.KeyCode == Keys.Enter && lbKontakty.SelectedItem != null)
            {
                var elementWybrany = (Kontakt)lbKontakty.SelectedItem;
                otworzOknoCzat(elementWybrany.ID);
            }
        }       

        // obługa zdarzeń interfejsu uzytkownika - koniec
        
    }
}
