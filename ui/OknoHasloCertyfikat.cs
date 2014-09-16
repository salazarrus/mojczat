﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MojCzat.ui
{
    public partial class OknoHasloCertyfikat : Form
    {
        public OknoHasloCertyfikat()
        {
            InitializeComponent();
            CenterToParent();
        }

        public string Haslo { get; set; }
        
        private void btnOK_Click(object sender, EventArgs e)
        {
            Haslo = tbHaslo.Text;
        }

    }
}
