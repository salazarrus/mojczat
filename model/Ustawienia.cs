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
    public class Ustawienia
    {
        public bool SSLWlaczone { get; set; }
        public string SSLCertyfikatSciezka { get; set; }
        public X509Certificate2 Certyfikat { get; set; }
        public string Opis { get; set; }

        public Ustawienia Kopiuj() {
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
                            ustawienia.Opis = dajAtrybut(wezel, "opis");
                            break;
                        case "ssl":
                            ustawienia.SSLWlaczone = (dajAtrybut(wezel, "wlaczone").ToLower() == "true");
                            ustawienia.SSLCertyfikatSciezka = dajAtrybut(wezel, "certyfikat");
                            break;
                    }
                }
            }
            catch { }
            return ustawienia;
        }

        public void Zapisz(string sciezkaPliku)
        {
            XmlDocument plikXML = new XmlDocument();

            var elementGlowny = plikXML.CreateElement("ustawienia");
            var elementSSL = plikXML.CreateElement("ssl");

            dodajAtrybut(plikXML, elementSSL, "wlaczone", SSLWlaczone.ToString());
            dodajAtrybut(plikXML, elementSSL,"certyfikat",SSLCertyfikatSciezka);

            var elementOgolne = plikXML.CreateElement("ogolne");
            dodajAtrybut(plikXML, elementOgolne, "opis", Opis);

            plikXML.AppendChild(elementGlowny);
            elementGlowny.AppendChild(elementSSL);
            elementGlowny.AppendChild(elementOgolne);
            plikXML.Save(sciezkaPliku);
        }

        public override bool Equals(object obj)
        {
            var ustawienia = obj as Ustawienia;
            
            if (ustawienia != null) {
                return this.SSLCertyfikatSciezka == ustawienia.SSLCertyfikatSciezka &&
                    this.SSLWlaczone == ustawienia.SSLWlaczone;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        static string dajAtrybut(XmlNode wezel, string atrybut) {
            if (wezel == null || atrybut == null || wezel.Attributes[atrybut] == null) 
            { return string.Empty; }

            return wezel.Attributes[atrybut].InnerText;
        }

        static void dodajAtrybut(XmlDocument dokument ,XmlElement element, string atrybut, string wartosc) {
            var nowyAtrybut = dokument.CreateAttribute(atrybut);
            nowyAtrybut.InnerText = wartosc;
            element.Attributes.Append(nowyAtrybut);
        }
    }
}
