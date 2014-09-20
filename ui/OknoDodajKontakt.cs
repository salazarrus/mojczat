using MojCzat.model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MojCzat.ui
{
    public partial class OknoDodajKontakt : Form
    {
        public Kontakt NowyKontakt { get; private set; }
        
        public OknoDodajKontakt()
        {
            InitializeComponent();
            CenterToParent();
            NowyKontakt = new Kontakt();
        }

        public OknoDodajKontakt(Kontakt kontakt):this() 
        {
            tbIP.Text = kontakt.IP.ToString();
            tbNazwa.Text = kontakt.Nazwa;
            NowyKontakt = kontakt;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CenterToParent();
        }

        void btnDodaj_Click(object sender, EventArgs e)
        {
            IPAddress adres;
            string nazwa = this.tbNazwa.Text.Trim();

            if (this.tbNazwa.Text.Trim() == String.Empty) {
                MessageBox.Show("Pole nazwa nie moze byc puste.");
                return;
            }
            
            if (!IPAddress.TryParse(this.tbIP.Text, out adres)) {
                MessageBox.Show("Niepoprawy adres IP.");
                return;
            }

            NowyKontakt.Nazwa = nazwa;
            NowyKontakt.IP = adres;
            NowyKontakt.ID = adres.ToString();

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();

        }
        
        private void OknoDodajKontakt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
                Close();
            }
        }
    }
}
