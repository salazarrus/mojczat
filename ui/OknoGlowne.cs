using MojCzat.komunikacja;
using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;

namespace MojCzat.ui
{
    /// <summary>
    /// W tym oknie znajduje sie lista kontaktow oraz ogolne menu aplikacji
    /// </summary>
    public partial class OknoGlowne : Form
    {
        // obiektu tego uzywamy do otwierania okna czatu z innego watku
        ObluzNowaWiadomoscUI obsluzNowaWiadomoscUI;
        
        //  obiektu tego uzywamy do odswiezania okna z innego watku
        OdswiezOkno odswiezOknoDelegata;

        
        // nasza lista kontaktow 
        List<Kontakt> kontakty;

        Komunikator komunikator;
        
        // list otwartych okien czatu
        Dictionary<String, OknoCzat> oknaCzatu = new Dictionary<string, OknoCzat>();

        // zrodlo wizualnej listy kontaktow
        BindingSource listaZrodlo = new BindingSource();

        // czy jestesmy polaczeni
        bool polaczony;

        Ustawienia ustawienia;

        /// <summary>
        /// Konstruktor okna glownego
        /// </summary>
        /// <param name="listaKontaktow">elementy tej listy prezentowane sa w spisie kontaktow</param>
        /// <param name="komunikator">referencja do komunikatora potrzebna jest do zapisania 
        /// sie jako sluchacz wydarzen (nowa wiadomosc etc.)</param>
        public OknoGlowne(List<Kontakt> listaKontaktow, Ustawienia ustawienia)
        {
            // inicjalizacja elementow formy
            InitializeComponent();
                  
            // zapisujemy referencje
            this.kontakty = listaKontaktow;
            this.ustawienia = ustawienia;        
                       
            comboStatus.SelectedIndex = 1;

            // centralne ustawienie okna na ekranie
            CenterToScreen();
            
            // ustalamy naglowek okna
            ustawNaglowek();
                                 
            // zaladuj elementy obiektu "kontakty" do interfejsu uzytkownika
            listaZrodlo.DataSource = this.kontakty;
            lbKontakty.DataSource = listaZrodlo;

            // inicjalizacja delegaty do otwierania okna czatu 
            obsluzNowaWiadomoscUI = new ObluzNowaWiadomoscUI(obsluzWiadomosc);
            // inicjalizacja delegaty do odswiezania okna
            odswiezOknoDelegata = new OdswiezOkno(odswiezListeKontaktow);
        }

        // ponizsza delegata jest konieczna do otwierania nowego okna czatu z watku Komunikatora 
        delegate void ObluzNowaWiadomoscUI(Kontakt rozmowca, TypWiadomosci rodzaj, string wiadomosc);

        // gdy nastapila zmiana dostepnosci kontaktow, odswiezamy okno
        delegate void OdswiezOkno();

        //sprzatamy po sobie
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (polaczony) {
                komunikator.NowaWiadomosc -= komunikator_NowaWiadomosc;
                komunikator.ZmianaStanuPolaczenia -= komunikator_ZmianaStanuPolaczenia;
                rozlaczSie(); 
            }
        }

        // wyswietl okno do wpisania hasla certyfikatu SSL
        string poprosHasloCertyfikatu(){
            var oknoHaslo = new OknoHasloCertyfikat();
            var wynik = oknoHaslo.ShowDialog(this);
            return (wynik == System.Windows.Forms.DialogResult.OK) ? 
                oknoHaslo.Haslo : null;
        }

        // daj instancje komunikatora
        Komunikator dajKomunikator() {
            var mapaAdresowIpKontaktow = new Dictionary<string, IPAddress>();
            kontakty.ForEach(k => mapaAdresowIpKontaktow.Add(k.ID, k.IP));

            if (ustawienia.SSLWlaczone && ustawienia.Certyfikat == null) 
            { 
                var haslo = poprosHasloCertyfikatu();
                if (haslo == null)
                {
                    MessageBox.Show("Brak hasła do certyfikatu uniemożliwia połączenie.");
                    return null;
                }
                
                try{
                    ustawienia.Certyfikat = new X509Certificate2(ustawienia.SSLCertyfikatSciezka, haslo);
                }catch(CryptographicException ex){
                    MessageBox.Show("Nastąpił błąd podczas otwierania certyfikatu: " + ex.Message);
                    return null;
                }
            }

            // zainicjalizuj obiekt odpowiedzialny za przesylanie / odbieranie wiadomosci
            var komunikator = new Komunikator(mapaAdresowIpKontaktow, ustawienia);
            return komunikator;
        }

        // ustawiony status "Dostepny"
        void polaczSie() {
            // uruchom oddzielny watek dla obiektu odpowiedzialnego za komunikacje
            komunikator = dajKomunikator();
            if (komunikator == null) 
            {
                comboStatus.SelectedIndex = 1;
                return; 
            }

            polaczony = true;
            // zapisujemy sie jako sluchacz wydarzenia NowaWiadomosc
            komunikator.NowaWiadomosc += komunikator_NowaWiadomosc;
            // zapisujemy sie jako sluchacz wydarzenia ZmianaStanuPolaczenia
            komunikator.ZmianaStanuPolaczenia += komunikator_ZmianaStanuPolaczenia;

            komunikator.Opis = ustawienia.Opis;
            // nawiaz polaczenia z kontaktami
            komunikator.Start();
           
            oknaCzatu.Values.ToList().ForEach(o => o.Komunikator = komunikator);
        }

        // ustawiony status "Niedostepny"
        void rozlaczSie() {
            // glowne okno programu zostalo zamkniete, dlatego zatrzymujemy dzialanie
            // obiektu odpowiedzialnego za komunikacje
            komunikator.Stop();
            polaczony = false;
            
            // zaktualizuj obiekt w oknach czat
            oknaCzatu.Values.ToList().ForEach(o => o.Komunikator = null);            
            komunikator = null;
        }

        // Ktos sie z nami polaczyl lub rozlaczyl, odswiezmy liste kontaktow
        void komunikator_ZmianaStanuPolaczenia(string rozmowca)
        {
            if (!kontakty.Any(k => k.ID == rozmowca)) { return; }
            if (komunikator.CzyDostepny(rozmowca)) { komunikator.PoprosOpis(rozmowca); }
                
            odswiezStatusKontaktu(rozmowca);
            Invoke(odswiezOknoDelegata);
        }
                   
        // Odswiez wizualna liste kontaktow
        void odswiezListeKontaktow() {
            listaZrodlo.DataSource = kontakty;
            listaZrodlo.ResetBindings(false);
        }

        // otrzymalismy nowa wiadomosc
        void komunikator_NowaWiadomosc(string idUzytkownika, TypWiadomosci rodzaj, string wiadomosc)
        {
            // otworz okno przez delegate poniewaz jestesmy w innym watku
            var kontakt = kontakty.Where(k => k.ID == idUzytkownika).SingleOrDefault();
            if (kontakt == null) // nieznany
            { 
                kontakt = new Kontakt() { ID = idUzytkownika, IP = IPAddress.Parse(idUzytkownika),
                    Nazwa = idUzytkownika, Polaczony = true };
                kontakty.Add(kontakt);
                Invoke(odswiezOknoDelegata);
                if (komunikator.CzyDostepny(idUzytkownika)) { komunikator.PoprosOpis(idUzytkownika); }
            }
             Invoke(obsluzNowaWiadomoscUI, kontakt, rodzaj , wiadomosc); 
        }
        
        // Polaczenie z innym uzytkownikiem zmienilo status, reagujemy
        void odswiezStatusKontaktu(string idRozmowcy){
            var kontakt = kontakty.Where(k => k.ID == idRozmowcy).SingleOrDefault();
            if (kontakt == null) { return; }
            kontakt.Polaczony = komunikator.CzyDostepny(idRozmowcy);
            
            // sortowanie - najpierw dostepni, potem kolejosc alfabetyczna
            this.kontakty = kontakty.OrderByDescending(k=>k.StatusTekst).ThenBy(k=>k.Nazwa).ToList();
        }

        void obsluzWiadomosc(Kontakt rozmowca, TypWiadomosci rodzaj , string wiadomosc) {
            if (rodzaj == TypWiadomosci.Zwykla) { otworzOknoCzat(rozmowca, wiadomosc); }
            else if (rodzaj == TypWiadomosci.Opis) { zmienOpisKontaktu(rozmowca, wiadomosc); }
        }

        // dostalismy nowy opis innego uzytkownia, pokazmy go
        void zmienOpisKontaktu(Kontakt rozmowca, string opis) {
            rozmowca.Opis = opis;
            odswiezListeKontaktow();
        }
        
        // Pokaz okno czatu z innym uzytkownikiem i wyswietl w nim wiadomosc
        void otworzOknoCzat(Kontakt rozmowca, string wiadomosc)
        {
            var okno = otworzOknoCzat(rozmowca);
            okno.WyswietlWiadomosc(wiadomosc);
        }
        
        // Pokaz okno czatu z innym uzytkownikiem. Jesli okno jeszcze nie istnieje, stworz je
        OknoCzat otworzOknoCzat(Kontakt rozmowca) {
            // jesli okno czatu jest juz otware, pokaz je
            if (oknaCzatu.ContainsKey(rozmowca.ID))
            { 
                // znajdz okno
                OknoCzat otwarteOkno = oknaCzatu[rozmowca.ID];
                // jesli okno jest zminimalizowane, przywroc je na pulpit
                if (otwarteOkno.WindowState == FormWindowState.Minimized) 
                { otwarteOkno.WindowState = FormWindowState.Normal; }
                // pokaz okno na pierwszym planie
                otwarteOkno.BringToFront();
                // pokaz je, jesli bylo schowane
                otwarteOkno.Visible = true;
            }
            else //nie bylo otwarte, wiec otworz nowe
            {
                OknoCzat noweOkno = new OknoCzat(rozmowca);
                noweOkno.Komunikator = komunikator;
                // zachowujemy nowe okno na liscie otwartych okien
                oknaCzatu.Add(rozmowca.ID, noweOkno);
                // pokazujemy nowe okno
                noweOkno.Show(this);
            }
            return oknaCzatu[rozmowca.ID];
        }

        // dodano / zmieniono kontakt 
        void dodajNowyKontakt(Kontakt kontakt) {
            if (kontakty.Any(k => k.ID == kontakt.ID)) 
            {
                MessageBox.Show("Masz już taki kontakt.");
                return;
            } 
            kontakty.Add(kontakt);

            if (polaczony)
            { 
                komunikator.DodajKontakt(kontakt.ID, kontakt.IP);
                kontakt.Polaczony = komunikator.CzyDostepny(kontakt.ID);
                komunikator.PoprosOpis(kontakt.ID);
            }            
            
            odswiezListeKontaktow();
            Kontakt.ZapiszListeKontaktow(kontakty, "kontakty.xml");
        }

        // naglowek okna
        void ustawNaglowek() {
            this.Text = "Mój Czat";
            if (!String.IsNullOrWhiteSpace(ustawienia.Opis)) { this.Text += String.Format(" ({0})", ustawienia.Opis); }
        }

        
        // klikniecie na przycisk "Dodaj"
        void btnDodaj_Click(object sender, EventArgs e)
        {
            var okno = new OknoDodajKontakt();
            var wynik = okno.ShowDialog(this);
            if (wynik == System.Windows.Forms.DialogResult.OK)
            {
                dodajNowyKontakt(okno.NowyKontakt);
            }
        }

        // klikniecie na przycisk "Usun"
        private void btnUsun_Click(object sender, EventArgs e)
        {
            if (lbKontakty.SelectedItem == null) { return; }
            
            var kontakt = (Kontakt)lbKontakty.SelectedItem;
            kontakty.RemoveAll(k => k.ID == kontakt.ID);
            if (polaczony) { 
                komunikator.Rozlacz(kontakt.ID);
                komunikator.UsunKontakt(kontakt.ID);                
            }

            odswiezListeKontaktow();
            Kontakt.ZapiszListeKontaktow(kontakty, "kontakty.xml");
        }


        // klikniecie na przycisk "Ustawienia"
        private void btnUstawienia_Click(object sender, EventArgs e)
        {
            var okno = new OknoUstawienia(ustawienia);
            var wynik = okno.ShowDialog(this);
            if (wynik != System.Windows.Forms.DialogResult.OK || okno.Ustawienia == ustawienia) { return; }

            ustawienia = okno.Ustawienia;
            ustawienia.Zapisz("ustawienia.xml");
            if (polaczony) {
                rozlaczSie();
                polaczSie();
            }
        }

        
        // Dwukrotne klikniecie na liste kontaktow
        private void lbKontatky_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            
            if (lbKontakty.IndexFromPoint(e.Location) == System.Windows.Forms.ListBox.NoMatches)
            { return; } // nie trafilismy w element listy
            
            if (lbKontakty.SelectedItem != null) 
            {
                var elementWybrany = (Kontakt)lbKontakty.SelectedItem;
                otworzOknoCzat(elementWybrany); 
            }
        }

        // Wcisniecie klawisza "Enter" w polu listy kontaktow
        private void lbKontatky_KeyDown(object sender, KeyEventArgs e)
        {
            // reagujemy na klawisz Enter
            if (e.KeyCode == Keys.Enter && lbKontakty.SelectedItem != null)
            {
                var elementWybrany = (Kontakt)lbKontakty.SelectedItem;
                otworzOknoCzat(elementWybrany);
            }
        }

        // zmiana statusu wlasnego
        private void comboStatus_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStatus.SelectedIndex == 0 &&
                komunikator == null) 
            {
                polaczSie();
                odswiezListeKontaktow();
            } 
            else if (comboStatus.SelectedIndex == 1 && polaczony) 
            {
                rozlaczSie();
                foreach (var kontakt in kontakty) { kontakt.Polaczony = false; }
                odswiezListeKontaktow();
            }    
        }

        // klikniecie przycisku "ustaw" opis
        private void btnUstawOpis_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(tbOpis.Text)) { return; }
            var nowyOpis = this.tbOpis.Text.Trim();
            ustawienia.Opis = nowyOpis;
            ustawienia.Zapisz("ustawienia.xml");
            ustawNaglowek();

            if (polaczony) {
                komunikator.Opis = nowyOpis;
                komunikator.OglosOpis(); 
            }
        }

        // klikniecie przycisku "zmien"
        private void btnZmien_Click(object sender, EventArgs e)
        {
            if (lbKontakty.SelectedItem == null){ return; }

            var elementWybrany = (Kontakt)lbKontakty.SelectedItem;

            var okno = new OknoDodajKontakt(elementWybrany);
            var wynik = okno.ShowDialog(this);
            if (wynik == System.Windows.Forms.DialogResult.OK)
            {
                kontakty.RemoveAll(k => k.ID == elementWybrany.ID);
                dodajNowyKontakt(okno.NowyKontakt); 
            }
        }              
    }
}
