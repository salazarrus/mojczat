using MojCzat.model;
using System.Drawing;
using System.Windows.Forms;

namespace MojCzat.ui
{
    /// <summary>
    /// Graficzne przedstawienie listy kontaktow
    /// </summary>
    [System.ComponentModel.DesignerCategory("")] 
    partial class ListaKontaktowUI: ListBox
    {
        private Size rozmiarIkony;
        private StringFormat format;
        private Font czcionkaTytul;
        private Font czcionkaDetal;

        public ListaKontaktowUI(Font czcionkaTytul, Font czcionkaDetal, Size rozmiarIkony, 
                         StringAlignment ulozenie, StringAlignment ulozeniePionowe)
        {
            this.czcionkaTytul = czcionkaTytul;
            this.czcionkaDetal = czcionkaDetal;
            this.rozmiarIkony = rozmiarIkony;
            this.ItemHeight = rozmiarIkony.Height + this.Margin.Vertical;
            format = new StringFormat();
            format.Alignment = ulozenie;
            format.LineAlignment = ulozeniePionowe;
        }

        public ListaKontaktowUI()
        {
            InitializeComponent();
            rozmiarIkony = new Size(80,60);
            this.ItemHeight = rozmiarIkony.Height + this.Margin.Vertical;
            this.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            format = new StringFormat();
            format.Alignment = StringAlignment.Near;
            format.LineAlignment = StringAlignment.Near;
            czcionkaTytul = new Font(this.Font, FontStyle.Bold);
            czcionkaDetal = new Font(this.Font, FontStyle.Regular);
            
        }
        
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // nic do narysowania
            if (this.Items.Count == 0) { return; }            
        
            // rysuj
            var kontakt = (Kontakt)this.Items[e.Index];  
            rysujElement(kontakt, e);
        }
       
        void rysujElement(Kontakt kontakt, DrawItemEventArgs e)
        {
            Padding margines = this.Margin;

            // kolorowanie wybranego elementu listy
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(Brushes.CornflowerBlue, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.Wheat, e.Bounds);
            }

            // separacja elementow listy
            e.Graphics.DrawLine(Pens.DarkGray, e.Bounds.X, e.Bounds.Y, e.Bounds.X + e.Bounds.Width, e.Bounds.Y);

            // rysowanie obrazka
           // Image obrazek = null;
            //e.Graphics.DrawImage(obrazek, e.Bounds.X + margines.Left, e.Bounds.Y + margines.Top, rozmiarIkony.Width, rozmiarIkony.Height);

            // obliczanie ram dla tytulu
            Rectangle ramyTytul = new Rectangle(e.Bounds.X + margines.Horizontal + rozmiarIkony.Width,
                                                  e.Bounds.Y + margines.Top,
                                                  e.Bounds.Width - margines.Right - rozmiarIkony.Width - margines.Horizontal,
                                                  (int)czcionkaTytul.GetHeight() + 2);

            // obliczanie ram dla detalu
            Rectangle ramyDetal = new Rectangle(e.Bounds.X + margines.Horizontal + rozmiarIkony.Width,
                                                   e.Bounds.Y + (int)czcionkaTytul.GetHeight() + 2 + margines.Vertical + margines.Top,
                                                   e.Bounds.Width - margines.Right - rozmiarIkony.Width - margines.Horizontal,
                                                   e.Bounds.Height - margines.Bottom - (int)czcionkaTytul.GetHeight() - 2 - margines.Vertical - margines.Top);

            // rysuj tekst
            e.Graphics.DrawString(kontakt.ID, czcionkaTytul, Brushes.Black, ramyTytul, format);
            e.Graphics.DrawString(kontakt.Dostepny ? "Dostępny" : "Niedostępny", czcionkaDetal, Brushes.DarkGray, ramyDetal, format);

            e.DrawFocusRectangle();
        }
    }
}
