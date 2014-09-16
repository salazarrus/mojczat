using System;
using System.Collections.Generic;
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


        /// <summary>
        /// Wczytaj liste kontaktow z pliku XML
        /// </summary>
        /// <param name="sciezkaPliku">sciezka do pliku</param>
        /// <returns></returns>
        public static Ustawienia Wczytaj(string sciezkaPliku)
        {
            Ustawienia ustawienia = new Ustawienia();
            XmlDocument plikXML = new XmlDocument();
            try
            {
                plikXML.Load(sciezkaPliku);

                foreach (XmlNode node in plikXML.DocumentElement.ChildNodes)
                {
                    switch (node.Name.ToLower())
                    {
                        case "ssl":
                            ustawienia.SSLWlaczone =
                                (node.Attributes["wlaczone"].InnerText.ToLower() == "true");
                            ustawienia.SSLCertyfikatSciezka = node.Attributes["certyfikat"].InnerText;
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
            
            var atrybutSslWlaczone = plikXML.CreateAttribute("wlaczone");
            atrybutSslWlaczone.InnerText = SSLWlaczone.ToString();
            elementSSL.Attributes.Append(atrybutSslWlaczone);

            var atrybutSslSciezka = plikXML.CreateAttribute("certyfikat");
            atrybutSslSciezka.InnerText = SSLCertyfikatSciezka;
            elementSSL.Attributes.Append(atrybutSslSciezka);

            plikXML.AppendChild(elementGlowny);
            elementGlowny.AppendChild(elementSSL);
            plikXML.Save(sciezkaPliku);
        }




    }
}
