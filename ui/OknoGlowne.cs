using MojCzat.komunikacja;
using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace MojCzat.ui
{
    /// <summary>
    /// W tym oknie znajduje sie lista kontaktow oraz ogolne menu aplikacji
    /// </summary>
    public partial class OknoGlowne : Form
    {
        
        /// <summary>
        /// obiektu tego uzywamy do otwierania okna czatu z innego watku
        /// </summary>
        OtworzOknoCzatuZWiadomoscia otworzOknoCzatuDelegata;


        /// <summary>
        ///  obiektu tego uzywamy do odswiezania okna z innego watku
        /// </summary>
        OdswiezOkno odswiezOknoDelegata;

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
        Dictionary<String, OknoCzat> oknaCzatu = new Dictionary<string, OknoCzat>();

        BindingSource listaZrodlo = new BindingSource();

        Thread watekKomunikator;

        bool polaczony;

        /// <summary>
        /// Konstruktor okna glownego
        /// </summary>
        /// <param name="listaKontaktow">elementy tej listy prezentowane sa w spisie kontaktow</param>
        /// <param name="komunikator">referencja do komunikatora potrzebna jest do zapisania sie jako sluchacz wydarzen (nowa wiadomosc etc.)</param>
        public OknoGlowne(List<Kontakt> listaKontaktow)
        {
                  
            // zapisujemy referencje
            this.kontakty = listaKontaktow;
                   
            polaczSie();

            // inicjalizacja elementow formy
            InitializeComponent();

            comboStatus.SelectedIndex = 0;

            // centralne ustawienie okna na ekranie
            CenterToScreen();
            
            // ustalamy naglowek okna
            this.Text = String.Format("Mój Czat ({0})", ConfigurationManager.AppSettings["mojeId"]);
                                 
            // zaladuj elementy obiektu "kontakty" do interfejsu uzytkownika
            listaZrodlo.DataSource = this.kontakty;
            lbKontakty.DataSource = listaZrodlo;

            // inicjalizacja delegaty do otwierania okna czatu 
            otworzOknoCzatuDelegata = new OtworzOknoCzatuZWiadomoscia(otworzOknoCzat);
            // inicjalizacja delegaty do odswiezania okna
            odswiezOknoDelegata = new OdswiezOkno(odswiezListeKontaktow);
        }

      
        /// <summary>
        /// ponizsza delegata jest konieczna do otwierania nowego okna czatu z watku Komunikatora 
        /// </summary>
        /// <param name="id">Identyfikator rozmowcy</param>
        /// <param name="wiadomosc">Wiadomosc, ktora wpiszemy do okna czatu</param>
        delegate void OtworzOknoCzatuZWiadomoscia(string idRozmowcy, string wiadomosc);

        /// <summary>
        /// Gdy nastapila zmiana dostepnosci kontaktow, odswiezamy okno
        /// </summary>
        delegate void OdswiezOkno();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (polaczony) { rozlaczSie(); }
        }

        Komunikator dajKomunikator() {
            var mapaAdresowIpKontaktow = new Dictionary<string, IPEndPoint>();
            kontakty.ForEach(k => mapaAdresowIpKontaktow.Add(k.ID, k.PunktKontaktu));

            // zainicjalizuj obiekt odpowiedzialny za przesylanie / odbieranie wiadomosci
            var komunikator = new Komunikator(mapaAdresowIpKontaktow, true);
            return komunikator;
        }

        void polaczSie() {
            // uruchom oddzielny watek dla obiektu odpowiedzialnego za komunikacje
            komunikator = dajKomunikator();
            polaczony = true;
            // zapisujemy sie jako sluchacz wydarzenia NowaWiadomosc
            komunikator.NowaWiadomosc += komunikator_NowaWiadomosc;
            // zapisujemy sie jako sluchacz wydarzenia ZmianaStanuPolaczenia
            komunikator.ZmianaStanuPolaczenia += komunikator_ZmianaStanuPolaczenia;

            // nawiaz polaczenia z kontaktami
            polaczSieZKontaktami();            
            
            watekKomunikator = new Thread(komunikator.Start);
            watekKomunikator.Start();
            oknaCzatu.Values.ToList().ForEach(o => o.Komunikator = komunikator);
        }

        void rozlaczSie() {
            // glowne okno programu zostalo zamkniete, dlatego zatrzymujemy dzialanie
            // obiektu odpowiedzialnego za komunikacje
            komunikator.Stop();
            polaczony = false;
            // zakoncz watek obiektu odpowiedzialnego za komunikacje
            watekKomunikator.Join();
            // zaktualizuj obiekt w oknach czat
            oknaCzatu.Values.ToList().ForEach(o => o.Komunikator = null);            
            watekKomunikator = null;
            komunikator = null;
        }

        /// <summary>
        /// Ktos sie z nami polaczyl lub rozlaczyl, odswiezmy liste kontaktow
        /// </summary>
        void komunikator_ZmianaStanuPolaczenia(string rozmowca, bool polaczenieOtwarte)
        {
            odswiezStatusKontaktu(rozmowca, polaczenieOtwarte);
            Invoke(odswiezOknoDelegata);
        }

        /// <summary>
        /// Inicjacja polaczen z uzytkownikami z listy kontaktow
        /// </summary>
        void polaczSieZKontaktami() 
        {
            foreach (var kontakt in kontakty) {
                polaczSieZKontaktem(kontakt, false);
            }
        }

        /// <summary>
        /// Inicjowanie polaczenia z uzytkownikem
        /// </summary>
        /// <param name="kontakt"></param>
        void polaczSieZKontaktem(Kontakt kontakt, bool nowy) 
        {
            kontakt.Status = komunikator.ZainicjujPolaczenie(kontakt.ID) 
                ? "Dostepny" : "Niedostepny";        
        }
                        
        /// <summary>
        /// Odswiez liste kontaktow
        /// </summary>
        void odswiezListeKontaktow() {
            listaZrodlo.ResetBindings(false);
        }

        /// <summary>
        /// komunikator przekazal nam nowa wiadomosc
        /// </summary>
        /// <param name="id">Identyfikator nadawcy</param>
        /// <param name="wiadomosc">tresc wiadomosci</param>
         void komunikator_NowaWiadomosc(string id, string wiadomosc)
        {
            // otworz okno przez delegate poniewaz jestesmy w innym watku
            Invoke(otworzOknoCzatuDelegata, id, wiadomosc);
        }
        
        /// <summary>
        /// Polaczenie zmienilo status, reagujemy
        /// </summary>
        void odswiezStatusKontaktu(string idRozmowcy, bool polaczenieOtwarte){
            var kontakt = kontakty.Where(k => k.ID == idRozmowcy).SingleOrDefault();
            if (kontakt == null) { return; }
            kontakt.Status = polaczenieOtwarte ? "Dostepny" : "Niedostepny";
            
            // sortowanie - najpierw dostepni, potem kolejosc alfabetyczna
            this.kontakty = kontakty.OrderByDescending(k=>k.Status).ThenBy(k=>k.ID).ToList();
        }

        /// <summary>
        /// Pokaz okno czatu z innym uzytkownikiem i wyswietl w nim wiadomosc
        /// </summary>
        /// <param name="idRozmowcy">nadawca wiadomosci </param>
        /// <param name="wiadomosc">tresc wiadomosci</param>
        void otworzOknoCzat(string idRozmowcy, string wiadomosc)
        {
            var okno = otworzOknoCzat(idRozmowcy);
            okno.WyswietlWiadomosc(wiadomosc);
        }
        
        /// <summary>
        /// Pokaz okno czatu z innym uzytkownikiem. Jesli okno jeszcze nie istnieje, stworz je
        /// </summary>
        /// <param name="idRozmowcy">Identyfikator rozmowcy</param>
        OknoCzat otworzOknoCzat(string idRozmowcy) {
            // jesli okno czatu jest juz otware, pokaz je
            if (oknaCzatu.ContainsKey(idRozmowcy))
            { 
                // znajdz okno
                OknoCzat otwarteOkno = oknaCzatu[idRozmowcy];
                // jesli okno jest zminimalizowane, przywroc je na pulpit
                if (otwarteOkno.WindowState == FormWindowState.Minimized) 
                {
                    otwarteOkno.WindowState = FormWindowState.Normal;
                }
                // pokaz okno na pierwszym planie
                otwarteOkno.BringToFront();
                // pokaz je, jesli bylo schowane
                otwarteOkno.Visible = true;
            }
            else //nie bylo otwarte, wiec otworz nowe
            {
                OknoCzat noweOkno = new OknoCzat(idRozmowcy);
                noweOkno.Komunikator = komunikator;
                // zachowujemy nowe okno na liscie otwartych okien
                oknaCzatu.Add(idRozmowcy, noweOkno);
                // pokazujemy nowe okno
                noweOkno.Show(this);
            }
            return oknaCzatu[idRozmowcy];
        }

        void dodajNowyKontakt(Kontakt kontakt) {
            if (kontakty.Any(k => k.ID == kontakt.ID || k.PunktKontaktu == kontakt.PunktKontaktu)) 
            {
                // juz cos takiego mamy
                return;
            }
            kontakty.Add(kontakt);
            komunikator.DodajKontakt(kontakt.ID, kontakt.PunktKontaktu);
            polaczSieZKontaktem(kontakt, true);
            odswiezListeKontaktow();
            Kontakt.ZapiszListeKontaktow(kontakty, "kontakty.xml");
        }

        // obługa zdarzeń interfejsu uzytkownika - poczatek

        private void btnDodaj_Click(object sender, EventArgs e)
        {
            Kontakt nowyKontakt = new Kontakt();
            new OknoDodajKontakt(nowyKontakt).ShowDialog(this);
            if (nowyKontakt.ID != null && nowyKontakt.PunktKontaktu != null) {
                dodajNowyKontakt(nowyKontakt);
            }
        }

        private void btnUsun_Click(object sender, EventArgs e)
        {
            if (lbKontakty.SelectedItem == null) { return; }
            
            var kontakt = (Kontakt)lbKontakty.SelectedItem;
            kontakty.RemoveAll(k => k.ID == kontakt.ID &&
                k.PunktKontaktu == kontakt.PunktKontaktu);
            komunikator.Rozlacz(kontakt.ID);
            komunikator.UsunKontakt(kontakt.ID);                
            odswiezListeKontaktow();
            Kontakt.ZapiszListeKontaktow(kontakty, "kontakty.xml");
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

        private void comboStatus_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStatus.SelectedIndex == 0 &&
                komunikator == null && watekKomunikator == null) 
            {
                polaczSie();
                odswiezListeKontaktow();
            } else if (comboStatus.SelectedIndex == 1 && polaczony) 
            {
                rozlaczSie();
                
                foreach (var kontakt in kontakty) {
                    kontakt.Status = "Niedostepny";
                }
                odswiezListeKontaktow();
            }    
        }       

        // obługa zdarzeń interfejsu uzytkownika - koniec
        
    }
}
