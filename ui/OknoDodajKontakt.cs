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
        Kontakt nowyKontakt;

        public OknoDodajKontakt(Kontakt nowyKontakt)
        {
            InitializeComponent();
            CenterToParent();

            this.nowyKontakt = nowyKontakt;
        }

        private void btnDodaj_Click(object sender, EventArgs e)
        {
            nowyKontakt.ID = this.tbId.Text;
            nowyKontakt.PunktKontaktu = new IPEndPoint(IPAddress.Parse(tbIP.Text), int.Parse(tbPort.Text));
            Close();
        }

        private void btnAnuluj_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
