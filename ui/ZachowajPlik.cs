using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MojCzat.ui
{
    public partial class ZachowajPlik : Form
    {
        public ZachowajPlik(string nazwaPliku, string nazwaUzytkownika)
        {
            InitializeComponent();
            lblPlik.Text = string.Format("{0} (od {1})", nazwaPliku, nazwaUzytkownika);
        }
    }
}
