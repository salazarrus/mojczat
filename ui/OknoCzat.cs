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
        // Drugi uczestnik czatu
        Kontakt rozmowca;

        public Komunikator Komunikator { set; private get; }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rozmowca">drugi uczestnik czatu</param>
        public OknoCzat(Kontakt rozmowca)
        {           
            // inicjalizacja elementow graficznych okna
            InitializeComponent();
            // ustalanie naglowka okna
            this.Text = String.Format("Mój Czat z {0}", rozmowca.Nazwa);

            this.rozmowca = rozmowca;
            tbCzat.MaxLength = int.MaxValue;
        }
        
        /// <summary>
        /// Dodaj wiadomosc do rozmowy
        /// </summary>
        /// <param name="wiadomosc">tresc wiadomosci</param>
        public void WyswietlWiadomosc(string wiadomosc)
        {
            tbCzat.AppendText(String.Format("[{0}] {1}\n", rozmowca.Nazwa, wiadomosc));
        }

        // Centralne pozycjonowanie okna wzgledem OknaGlownego
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);            
            CenterToParent();
        }

        // Nie zamykaj okna, tylko je ukryj
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        // Wiadomosc zostala wyslana, czyscimy wiec pole wpisywania wiadomosci
        void wyczyscPoleWiadomosci(){
            // wyczysc pole z tekstu
            tbWiadomosc.Text = String.Empty;
            // przygotuj do wpisywania
            tbWiadomosc.Focus();
            // ustaw kursor w pozycji poczatkowej
            tbWiadomosc.Select(0, 0);               
        }

        // sprawdz czy istnieje polaczenie do rozmowcy
        bool moznaWysylac() 
        {
            if (Komunikator == null)
            {
                MessageBox.Show("Nie wysłano. Jestes niedostępny.");
                return false;
            }

            if (!Komunikator.CzyDostepny(rozmowca.ID))
            {
                MessageBox.Show("Nie wysłano. Twój rozmówca jest niedostępny.");
                return false;
            }
            return true;
        }

        // wysylamy nowa wiadomosc
        void wyslijWpisanaWiadomosc() {
            if (!moznaWysylac()) { return; }    
                
            String wiadomosc = tbWiadomosc.Text;

            // usuwamy biale znaki z lewej i prawej strony tekstu
            wiadomosc = wiadomosc.Trim();
            // nie chcemy wysylac pustych wiadomosci
            if (wiadomosc == String.Empty) { return; }

            // wysylamy wiadomosc
            Komunikator.WyslijWiadomosc(rozmowca.ID, wiadomosc);
            
            // dodajemy wiadomosc do naszego okna czatu
            tbCzat.AppendText(String.Format("[{0}] {1}\n", "Ty", wiadomosc));
            // czyscimy pole wpisywania dla nowej wiadomosci
            wyczyscPoleWiadomosci();
        }

        // wyslij plik do rozmowcy
        void wyslijPlik(string sciezka) 
        {
            if (!moznaWysylac()) { return; }
            Komunikator.WyslijPlik(rozmowca.ID, sciezka);
        }


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

        // "zamknij" okno na klawisz Escape
        void OknoCzat_KeyDown(object sender, KeyEventArgs e)
        { if (e.KeyCode == Keys.Escape) { Close(); } }

        // wyslij plik
        void btnWyslijPlik_Click(object sender, EventArgs e)
        {
            var wynik = ofdWyslij.ShowDialog(this);
            if (wynik != DialogResult.OK) { return; }

            wyslijPlik(ofdWyslij.FileName);
        }

    }
}
