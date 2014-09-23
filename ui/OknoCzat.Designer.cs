﻿namespace MojCzat.ui
{
    partial class OknoCzat
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OknoCzat));
            this.tbCzat = new System.Windows.Forms.TextBox();
            this.tbWiadomosc = new System.Windows.Forms.TextBox();
            this.btnWyslij = new System.Windows.Forms.Button();
            this.cbWyslijEnter = new System.Windows.Forms.CheckBox();
            this.btnWyslijPlik = new System.Windows.Forms.Button();
            this.ofdWyslij = new System.Windows.Forms.OpenFileDialog();
            this.SuspendLayout();
            // 
            // tbCzat
            // 
            this.tbCzat.BackColor = System.Drawing.SystemColors.Window;
            this.tbCzat.Location = new System.Drawing.Point(12, 12);
            this.tbCzat.Multiline = true;
            this.tbCzat.Name = "tbCzat";
            this.tbCzat.ReadOnly = true;
            this.tbCzat.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbCzat.Size = new System.Drawing.Size(403, 253);
            this.tbCzat.TabIndex = 3;
            // 
            // tbWiadomosc
            // 
            this.tbWiadomosc.Location = new System.Drawing.Point(12, 271);
            this.tbWiadomosc.MaxLength = 500;
            this.tbWiadomosc.Multiline = true;
            this.tbWiadomosc.Name = "tbWiadomosc";
            this.tbWiadomosc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbWiadomosc.Size = new System.Drawing.Size(403, 81);
            this.tbWiadomosc.TabIndex = 0;
            this.tbWiadomosc.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbWiadomosc_KeyDown);
            // 
            // btnWyslij
            // 
            this.btnWyslij.Location = new System.Drawing.Point(169, 358);
            this.btnWyslij.Name = "btnWyslij";
            this.btnWyslij.Size = new System.Drawing.Size(75, 23);
            this.btnWyslij.TabIndex = 1;
            this.btnWyslij.Text = "Wyślij";
            this.btnWyslij.UseVisualStyleBackColor = true;
            this.btnWyslij.Click += new System.EventHandler(this.btnWyslij_Click);
            // 
            // cbWyslijEnter
            // 
            this.cbWyslijEnter.AutoSize = true;
            this.cbWyslijEnter.Checked = true;
            this.cbWyslijEnter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbWyslijEnter.Location = new System.Drawing.Point(267, 395);
            this.cbWyslijEnter.Name = "cbWyslijEnter";
            this.cbWyslijEnter.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cbWyslijEnter.Size = new System.Drawing.Size(148, 17);
            this.cbWyslijEnter.TabIndex = 2;
            this.cbWyslijEnter.Text = "Wyślij po wciśnięciu Enter";
            this.cbWyslijEnter.UseVisualStyleBackColor = true;
            // 
            // btnWyslijPlik
            // 
            this.btnWyslijPlik.Location = new System.Drawing.Point(3, 395);
            this.btnWyslijPlik.Name = "btnWyslijPlik";
            this.btnWyslijPlik.Size = new System.Drawing.Size(75, 23);
            this.btnWyslijPlik.TabIndex = 4;
            this.btnWyslijPlik.Text = "Wyślij plik...";
            this.btnWyslijPlik.UseVisualStyleBackColor = true;
            this.btnWyslijPlik.Click += new System.EventHandler(this.btnWyslijPlik_Click);
            // 
            // OknoCzat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(427, 424);
            this.Controls.Add(this.btnWyslijPlik);
            this.Controls.Add(this.cbWyslijEnter);
            this.Controls.Add(this.btnWyslij);
            this.Controls.Add(this.tbWiadomosc);
            this.Controls.Add(this.tbCzat);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.Name = "OknoCzat";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OknoCzat_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbCzat;
        private System.Windows.Forms.TextBox tbWiadomosc;
        private System.Windows.Forms.Button btnWyslij;
        private System.Windows.Forms.CheckBox cbWyslijEnter;
        private System.Windows.Forms.Button btnWyslijPlik;
        private System.Windows.Forms.OpenFileDialog ofdWyslij;
    }
}