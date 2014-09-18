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
            this.btnHistoria = new System.Windows.Forms.Button();
            this.btnUstawienia = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboStatus = new System.Windows.Forms.ComboBox();
            this.btnUstawOpis = new System.Windows.Forms.Button();
            this.tbOpis = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lbKontakty = new MojCzat.ui.ListaKontaktowUI();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDodaj
            // 
            this.btnDodaj.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDodaj.Location = new System.Drawing.Point(74, 378);
            this.btnDodaj.Name = "btnDodaj";
            this.btnDodaj.Size = new System.Drawing.Size(75, 23);
            this.btnDodaj.TabIndex = 4;
            this.btnDodaj.Text = "+";
            this.btnDodaj.UseVisualStyleBackColor = true;
            this.btnDodaj.Click += new System.EventHandler(this.btnDodaj_Click);
            // 
            // btnUsun
            // 
            this.btnUsun.Location = new System.Drawing.Point(155, 378);
            this.btnUsun.Name = "btnUsun";
            this.btnUsun.Size = new System.Drawing.Size(75, 23);
            this.btnUsun.TabIndex = 5;
            this.btnUsun.Text = "----";
            this.btnUsun.UseVisualStyleBackColor = true;
            this.btnUsun.Click += new System.EventHandler(this.btnUsun_Click);
            // 
            // btnHistoria
            // 
            this.btnHistoria.Location = new System.Drawing.Point(162, 12);
            this.btnHistoria.Name = "btnHistoria";
            this.btnHistoria.Size = new System.Drawing.Size(75, 23);
            this.btnHistoria.TabIndex = 2;
            this.btnHistoria.Text = "Historia";
            this.btnHistoria.UseVisualStyleBackColor = true;
            this.btnHistoria.Click += new System.EventHandler(this.btnHistoria_Click);
            // 
            // btnUstawienia
            // 
            this.btnUstawienia.Location = new System.Drawing.Point(243, 12);
            this.btnUstawienia.Name = "btnUstawienia";
            this.btnUstawienia.Size = new System.Drawing.Size(75, 23);
            this.btnUstawienia.TabIndex = 3;
            this.btnUstawienia.Text = "Ustawienia";
            this.btnUstawienia.UseVisualStyleBackColor = true;
            this.btnUstawienia.Click += new System.EventHandler(this.btnUstawienia_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.lbKontakty);
            this.groupBox1.Controls.Add(this.btnDodaj);
            this.groupBox1.Controls.Add(this.btnUsun);
            this.groupBox1.Location = new System.Drawing.Point(14, 41);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 407);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Kontakty";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 487);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Status";
            // 
            // comboStatus
            // 
            this.comboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStatus.FormattingEnabled = true;
            this.comboStatus.Items.AddRange(new object[] {
            "Dostępny",
            "Niedostępny"});
            this.comboStatus.Location = new System.Drawing.Point(63, 484);
            this.comboStatus.Name = "comboStatus";
            this.comboStatus.Size = new System.Drawing.Size(100, 21);
            this.comboStatus.TabIndex = 5;
            this.comboStatus.SelectedValueChanged += new System.EventHandler(this.comboStatus_SelectedValueChanged);
            // 
            // btnUstawOpis
            // 
            this.btnUstawOpis.Location = new System.Drawing.Point(169, 458);
            this.btnUstawOpis.Name = "btnUstawOpis";
            this.btnUstawOpis.Size = new System.Drawing.Size(75, 23);
            this.btnUstawOpis.TabIndex = 6;
            this.btnUstawOpis.Text = "Ustaw";
            this.btnUstawOpis.UseVisualStyleBackColor = true;
            this.btnUstawOpis.Click += new System.EventHandler(this.btnUstawOpis_Click);
            // 
            // tbOpis
            // 
            this.tbOpis.Location = new System.Drawing.Point(63, 461);
            this.tbOpis.Name = "tbOpis";
            this.tbOpis.Size = new System.Drawing.Size(100, 20);
            this.tbOpis.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 460);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Opis:";
            // 
            // lbKontakty
            // 
            this.lbKontakty.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbKontakty.FormattingEnabled = true;
            this.lbKontakty.ItemHeight = 66;
            this.lbKontakty.Location = new System.Drawing.Point(11, 38);
            this.lbKontakty.Name = "lbKontakty";
            this.lbKontakty.Size = new System.Drawing.Size(293, 334);
            this.lbKontakty.TabIndex = 0;
            this.lbKontakty.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lbKontatky_KeyDown);
            this.lbKontakty.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbKontatky_MouseDoubleClick);
            // 
            // OknoGlowne
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 522);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbOpis);
            this.Controls.Add(this.btnUstawOpis);
            this.Controls.Add(this.comboStatus);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnUstawienia);
            this.Controls.Add(this.btnHistoria);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "OknoGlowne";
            this.Text = "Mój Czat";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnDodaj;
        private System.Windows.Forms.Button btnUsun;
        private System.Windows.Forms.Button btnHistoria;
        private System.Windows.Forms.Button btnUstawienia;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboStatus;
        private ListaKontaktowUI lbKontakty;
        private System.Windows.Forms.Button btnUstawOpis;
        private System.Windows.Forms.TextBox tbOpis;
        private System.Windows.Forms.Label label3;
    }
}