namespace MojCzat.ui
{
    partial class OknoGlowne
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OknoGlowne));
            this.btnDodaj = new System.Windows.Forms.Button();
            this.btnUsun = new System.Windows.Forms.Button();
            this.btnWyszukaj = new System.Windows.Forms.Button();
            this.btnHistoria = new System.Windows.Forms.Button();
            this.btnUstawienia = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbKontakty = new ListaKontaktowUI();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDodaj
            // 
            this.btnDodaj.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDodaj.Location = new System.Drawing.Point(73, 419);
            this.btnDodaj.Name = "btnDodaj";
            this.btnDodaj.Size = new System.Drawing.Size(75, 23);
            this.btnDodaj.TabIndex = 4;
            this.btnDodaj.Text = "+";
            this.btnDodaj.UseVisualStyleBackColor = true;
            this.btnDodaj.Click += new System.EventHandler(this.btnDodaj_Click);
            // 
            // btnUsun
            // 
            this.btnUsun.Location = new System.Drawing.Point(154, 419);
            this.btnUsun.Name = "btnUsun";
            this.btnUsun.Size = new System.Drawing.Size(75, 23);
            this.btnUsun.TabIndex = 5;
            this.btnUsun.Text = "----";
            this.btnUsun.UseVisualStyleBackColor = true;
            this.btnUsun.Click += new System.EventHandler(this.btnUsun_Click);
            // 
            // btnWyszukaj
            // 
            this.btnWyszukaj.Location = new System.Drawing.Point(25, 12);
            this.btnWyszukaj.Name = "btnWyszukaj";
            this.btnWyszukaj.Size = new System.Drawing.Size(75, 23);
            this.btnWyszukaj.TabIndex = 0;
            this.btnWyszukaj.Text = "Wyszukaj";
            this.btnWyszukaj.UseVisualStyleBackColor = true;
            this.btnWyszukaj.Click += new System.EventHandler(this.btnWyszukaj_Click);
            // 
            // btnHistoria
            // 
            this.btnHistoria.Location = new System.Drawing.Point(106, 12);
            this.btnHistoria.Name = "btnHistoria";
            this.btnHistoria.Size = new System.Drawing.Size(75, 23);
            this.btnHistoria.TabIndex = 2;
            this.btnHistoria.Text = "Historia";
            this.btnHistoria.UseVisualStyleBackColor = true;
            this.btnHistoria.Click += new System.EventHandler(this.btnHistoria_Click);
            // 
            // btnUstawienia
            // 
            this.btnUstawienia.Location = new System.Drawing.Point(187, 12);
            this.btnUstawienia.Name = "btnUstawienia";
            this.btnUstawienia.Size = new System.Drawing.Size(75, 23);
            this.btnUstawienia.TabIndex = 3;
            this.btnUstawienia.Text = "Ustawienia";
            this.btnUstawienia.UseVisualStyleBackColor = true;
            this.btnUstawienia.Click += new System.EventHandler(this.btnUstawienia_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbKontakty);
            this.groupBox1.Controls.Add(this.btnDodaj);
            this.groupBox1.Controls.Add(this.btnUsun);
            this.groupBox1.Location = new System.Drawing.Point(14, 41);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 469);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // lbKontatky
            // 
            this.lbKontakty.FormattingEnabled = true;
            this.lbKontakty.Location = new System.Drawing.Point(11, 19);
            this.lbKontakty.Name = "lbKontatky";
            this.lbKontakty.Size = new System.Drawing.Size(293, 381);
            this.lbKontakty.TabIndex = 0;
            this.lbKontakty.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lbKontatky_KeyDown);
            this.lbKontakty.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbKontatky_MouseDoubleClick);
            // 
            // OknoGlowne
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 522);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnUstawienia);
            this.Controls.Add(this.btnWyszukaj);
            this.Controls.Add(this.btnHistoria);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "OknoGlowne";
            this.Text = "Mój Czat";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDodaj;
        private System.Windows.Forms.Button btnUsun;
        private System.Windows.Forms.Button btnWyszukaj;
        private System.Windows.Forms.Button btnHistoria;
        private System.Windows.Forms.Button btnUstawienia;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lbKontakty;
    }
}