namespace MojCzat.ui
{
    partial class OknoUstawienia
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OknoUstawienia));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnWybierz = new System.Windows.Forms.Button();
            this.tbCertyfikat = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chBoxWlaczSSL = new System.Windows.Forms.CheckBox();
            this.btnZapisz = new System.Windows.Forms.Button();
            this.btnAnuluj = new System.Windows.Forms.Button();
            this.ofdCertyfikatPFX = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnWybierz);
            this.groupBox1.Controls.Add(this.tbCertyfikat);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.chBoxWlaczSSL);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(343, 77);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "SSL";
            // 
            // btnWybierz
            // 
            this.btnWybierz.Location = new System.Drawing.Point(264, 32);
            this.btnWybierz.Name = "btnWybierz";
            this.btnWybierz.Size = new System.Drawing.Size(75, 23);
            this.btnWybierz.TabIndex = 3;
            this.btnWybierz.Text = "Wybierz...";
            this.btnWybierz.UseVisualStyleBackColor = true;
            this.btnWybierz.Click += new System.EventHandler(this.btnWybierz_Click);
            // 
            // tbCertyfikat
            // 
            this.tbCertyfikat.BackColor = System.Drawing.SystemColors.Window;
            this.tbCertyfikat.Location = new System.Drawing.Point(66, 35);
            this.tbCertyfikat.Name = "tbCertyfikat";
            this.tbCertyfikat.Size = new System.Drawing.Size(192, 20);
            this.tbCertyfikat.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Plik .pfx";
            // 
            // chBoxWlaczSSL
            // 
            this.chBoxWlaczSSL.AutoSize = true;
            this.chBoxWlaczSSL.Location = new System.Drawing.Point(19, 19);
            this.chBoxWlaczSSL.Name = "chBoxWlaczSSL";
            this.chBoxWlaczSSL.Size = new System.Drawing.Size(81, 17);
            this.chBoxWlaczSSL.TabIndex = 0;
            this.chBoxWlaczSSL.Text = "Włącz SSL";
            this.chBoxWlaczSSL.UseVisualStyleBackColor = true;
            this.chBoxWlaczSSL.CheckedChanged += new System.EventHandler(this.chBoxWlaczSSL_CheckedChanged);
            // 
            // btnZapisz
            // 
            this.btnZapisz.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnZapisz.Location = new System.Drawing.Point(109, 106);
            this.btnZapisz.Name = "btnZapisz";
            this.btnZapisz.Size = new System.Drawing.Size(75, 23);
            this.btnZapisz.TabIndex = 4;
            this.btnZapisz.Text = "Zapisz";
            this.btnZapisz.UseVisualStyleBackColor = true;
            this.btnZapisz.Click += new System.EventHandler(this.btnZapisz_Click);
            // 
            // btnAnuluj
            // 
            this.btnAnuluj.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAnuluj.Location = new System.Drawing.Point(190, 106);
            this.btnAnuluj.Name = "btnAnuluj";
            this.btnAnuluj.Size = new System.Drawing.Size(75, 23);
            this.btnAnuluj.TabIndex = 5;
            this.btnAnuluj.Text = "Anuluj";
            this.btnAnuluj.UseVisualStyleBackColor = true;
            this.btnAnuluj.Click += new System.EventHandler(this.btnAnuluj_Click);
            // 
            // OknoUstawienia
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(368, 141);
            this.Controls.Add(this.btnAnuluj);
            this.Controls.Add(this.btnZapisz);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OknoUstawienia";
            this.Text = "Ustawienia";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chBoxWlaczSSL;
        private System.Windows.Forms.Button btnWybierz;
        private System.Windows.Forms.TextBox tbCertyfikat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnZapisz;
        private System.Windows.Forms.Button btnAnuluj;
        private System.Windows.Forms.OpenFileDialog ofdCertyfikatPFX;
    }
}