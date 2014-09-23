using MojCzat.uzytki;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MojCzat.model
{
    /// <summary>
    /// Obiekt przechowujacy dlugo- oraz krotkotrwale ustawienia komunikatora
    /// </summary>
    public class Ustawienia
    {
        /// <summary>
        /// Czy komunikacja przebiega przy uzyciu SSL
        /// </summary>
        public bool SSLWlaczone { get; set; }

        /// <summary>
        /// Sciezka do pliku .pfx z certyfikatem, ktorego uzywamy do oferowania polaczen SSL
        /// </summary>
        public string SSLCertyfikatSciezka { get; set; }


        /// <summary>
        /// Wczytany certyfikat
        /// </summary>
        public X509Certificate2 Certyfikat { get; set; }


        /// <summary>
        /// Opis wlasny
        /// </summary>
        public string Opis { get; set; }

        /// <summary>
        /// Kopiowanie obiektu
        /// </summary>
        /// <returns></returns>
        public Ustawienia Kopiuj()
        {
            return new Ustawienia()
            {
                SSLCertyfikatSciezka = this.SSLCertyfikatSciezka,
                SSLWlaczone = this.SSLWlaczone,
                Opis = this.Opis
            };
        }

        /// <summary>
        /// Wczytaj liste kontaktow z pliku XML
        /// </summary>
        /// <param name="sciezkaPliku">sciezka do pliku</param>
        /// <returns></returns>
        public static Ustawienia Wczytaj(string sciezkaPliku)
        {
            Ustawienia ustawienia = new Ustawienia();

            if (sciezkaPliku == null || !File.Exists(sciezkaPliku)) { return ustawienia; }

            XmlDocument plikXML = new XmlDocument();
            try
            {
                plikXML.Load(sciezkaPliku);

                foreach (XmlNode wezel in plikXML.DocumentElement.ChildNodes)
                {
                    switch (wezel.Name.ToLower())
                    {
                        case "ogolne":
                            ustawienia.Opis = Xml.DajAtrybut(wezel, "opis");
                            break;
                        case "ssl":
                            ustawienia.SSLWlaczone = (Xml.DajAtrybut(wezel, "wlaczone").ToLower() == "true");
                            ustawienia.SSLCertyfikatSciezka = Xml.DajAtrybut(wezel, "certyfikat");
                            break;
                    }
                }
            }
            catch { }
            return ustawienia;
        }

        /// <summary>
        /// Zapisz ustawienia do pliku
        /// </summary>
        /// <param name="sciezkaPliku"></param>
        public void Zapisz(string sciezkaPliku)
        {
            XmlDocument plikXML = new XmlDocument();

            var elementGlowny = plikXML.CreateElement("ustawienia");
            var elementSSL = plikXML.CreateElement("ssl");

            Xml.DodajAtrybut(plikXML, elementSSL, "wlaczone", SSLWlaczone.ToString());
            Xml.DodajAtrybut(plikXML, elementSSL, "certyfikat", SSLCertyfikatSciezka);

            var elementOgolne = plikXML.CreateElement("ogolne");
            Xml.DodajAtrybut(plikXML, elementOgolne, "opis", Opis);

            plikXML.AppendChild(elementGlowny);
            elementGlowny.AppendChild(elementSSL);
            elementGlowny.AppendChild(elementOgolne);
            plikXML.Save(sciezkaPliku);
        }

        public override bool Equals(object obj)
        {
            var ustawienia = obj as Ustawienia;

            if (ustawienia != null)
            {
                return this.SSLCertyfikatSciezka == ustawienia.SSLCertyfikatSciezka &&
                    this.SSLWlaczone == ustawienia.SSLWlaczone;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
