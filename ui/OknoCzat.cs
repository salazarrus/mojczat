#define TRACE

using MojCzat.komunikacja;
using MojCzat.model;
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
        /// Drugi uczestnik czatu
        /// </summary>
        Kontakt rozmowca;

        /// <summary>
        /// Obiekt odpowiedzialny za przesylanie i odbieranie wiadomosci
        /// </summary>
        Komunikator komunikator;


        public Komunikator Komunikator { set { komunikator = value; } }

        /// <summary>
        /// Konstruktor okna czatu
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator drugiego uczestnika czatu</param>
        /// <param name="komunikator">Obiekt odpowiedzialny za przesylanie i odbieranie wiadomosci</param>
        public OknoCzat(Kontakt rozmowca)
        {           
            // inicjalizacja elementow graficznych okna
            InitializeComponent();
            // ustalanie naglowka okna
            this.Text = String.Format("Mój Czat z {0}", rozmowca.Nazwa);

            // zapisywanie referencji
            this.rozmowca = rozmowca;
        }
        
        /// <summary>
        /// Dodaj wiadomosc do rozmowy
        /// </summary>
        /// <param name="wiadomosc">tresc wiadomosci</param>
        public void WyswietlWiadomosc(string wiadomosc)
        {
            tbCzat.AppendText(String.Format("[{0}] {1}\n", rozmowca.Nazwa, wiadomosc));
        }

        /// <summary>
        /// Centralne pozycjonowanie okna wzgledem OknaGlownego
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);            
            CenterToParent();
        }

        /// <summary>
        /// Nie zamykaj okna, tylko je ukryj
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        /// <summary>
        /// Wiadomosc zostala wyslana, czyscimy wiec pole wpisywania wiadomosci
        /// </summary>
        void wyczyscPoleWiadomosci(){
            // wyczysc pole z tekstu
            tbWiadomosc.Text = String.Empty;
            // przygotuj do wpisywania
            tbWiadomosc.Focus();
            // ustaw kursor w pozycji poczatkowej
            tbWiadomosc.Select(0, 0);               
        }

        bool moznaWysylac() 
        {
            if (komunikator == null)
            {
                MessageBox.Show("Nie wysłano. Jestes niedostępny.");
                return false;
            }

            if (!komunikator.CzyDostepny(rozmowca.ID))
            {
                MessageBox.Show("Nie wysłano. Twój rozmówca jest niedostępny.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// wysylamy nowa wiadomosc
        /// </summary>
        void wyslijWpisanaWiadomosc() {
            if (!moznaWysylac()) { return; }    
                
            String wiadomosc = tbWiadomosc.Text;

            // usuwamy biale znaki z lewej i prawej strony tekstu
            wiadomosc = wiadomosc.Trim();
            // nie chcemy wysylac pustych wiadomosci
            if (wiadomosc == String.Empty) { return; }

            //TODO: sprawdz czy dostepny
             
            // wysylamy wiadomosc
            komunikator.WyslijWiadomosc(rozmowca.ID, wiadomosc);
            
            // dodajemy wiadomosc do naszego okna czatu
            tbCzat.AppendText(String.Format("[{0}] {1}\n", "Ty", wiadomosc));
            // czyscimy pole wpisywania dla nowej wiadomosci
            wyczyscPoleWiadomosci();
            
        }

        void wyslijPlik(string sciezka) 
        {
            if (!moznaWysylac()) { return; }
            komunikator.WyslijPlik(rozmowca.ID, sciezka);
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
                // nie chcemy nowej linii w polu wiadomosci
                e.SuppressKeyPress = true;
            }
            
        }

        void OknoCzat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) {
                Close();
            }
        }

        void btnWyslijPlik_Click(object sender, EventArgs e)
        {
            var wynik = ofdWyslij.ShowDialog(this);
            if (wynik != DialogResult.OK) { return; }

            wyslijPlik(ofdWyslij.FileName);
        }

        // obługa zdarzeń interfejsu uzytkownika - koniec
    }
}
