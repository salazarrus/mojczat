using MojCzat.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MojCzat.ui
{
    public partial class OknoUstawienia : Form
    {
        public Ustawienia Ustawienia { get; private set; }

        private Ustawienia pierwotne;

        public OknoUstawienia(Ustawienia obecne)
        {
            Ustawienia = obecne.Kopiuj();

            InitializeComponent();
            
            ustawObszarSSL();
        }
            

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CenterToParent();
        }

        
        void ustawObszarSSL(){            
            tbCertyfikat.Text = Ustawienia.SSLWlaczone?
                Ustawienia.SSLCertyfikatSciezka : String.Empty;

            chBoxWlaczSSL.Checked = tbCertyfikat.Enabled =
                btnWybierz.Enabled = Ustawienia.SSLWlaczone;
        }

        void btnWybierz_Click(object sender, EventArgs e)
        {
            var wynik = ofdCertyfikatPFX.ShowDialog(this);
            if (wynik != DialogResult.OK) { return; }

            tbCertyfikat.Text = ofdCertyfikatPFX.FileName;
        }

        private void btnZapisz_Click(object sender, EventArgs e)
        {
            if (chBoxWlaczSSL.Checked && !File.Exists(tbCertyfikat.Text)) 
            {
                MessageBox.Show("Podana sciezka do certyfikatu jest nieprawidlowa.");
                return;
            }

            Ustawienia.SSLWlaczone = chBoxWlaczSSL.Checked;
            Ustawienia.SSLCertyfikatSciezka = tbCertyfikat.Text;
            Close();
        }

        private void btnAnuluj_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void chBoxWlaczSSL_CheckedChanged(object sender, EventArgs e)
        {
            Ustawienia.SSLWlaczone = chBoxWlaczSSL.Checked;
            ustawObszarSSL();
        }
    }
}
