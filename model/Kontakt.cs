using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace MojCzat.model
{
    /// <summary>
    /// Obiekt reprezetujacy uzytkownika, z ktorym mozna prowadzic czat
    /// </summary>
    public class Kontakt
    {
        /// <summary>
        /// Identyfikator uzytkownika
        /// </summary>
        public String ID { get; set; }
 
        /// <summary>
        /// Adres IP uzytkownika
        /// </summary>
        public IPAddress IP { get; set; }
 
        /// <summary>
        /// Dostepnosc czatu z tym uzytkownikiem
        /// </summary>
        public bool Polaczony { get; set; }

        public String StatusTekst { 
            get {
                return Polaczony ? "Dostępny" : "Niedostępny";
            }        
        }

        /// <summary>
        /// Nazwa wyswietlana
        /// </summary>
        public string Nazwa { get; set; }


        public string Opis { get; set; }

        /// <summary>
        /// Wczytaj liste kontaktow z pliku XML
        /// </summary>
        /// <param name="sciezkaPliku">sciezka do pliku</param>
        /// <returns></returns>
        public static List<Kontakt> WczytajListeKontaktow(string sciezkaPliku) {
            List<Kontakt> listaWynikowa = new List<Kontakt>();
            XmlDocument plikXML = new XmlDocument();
            try 
            { 
                plikXML.Load(sciezkaPliku); 
                foreach (XmlNode node in plikXML.DocumentElement.ChildNodes)
                {
                    
                    string ip = node.Attributes["ip"].InnerText;
                    string id = ip;
                    string nazwa = node.Attributes["nazwa"].InnerText;
                    listaWynikowa.Add(new Kontakt() { ID = id, IP = IPAddress.Parse(ip), Nazwa = nazwa ,Polaczony=false });
                }
            } catch { return listaWynikowa; }

            return listaWynikowa;        
        }


        /// <summary>
        /// Zapisz liste kontaktow do pliku XML
        /// </summary>
        /// <param name="lista">lista do zapisania</param>
        /// <param name="sciezkaPliku"></param>
        public static void ZapiszListeKontaktow(List<Kontakt> lista, string sciezkaPliku)
        {
            XmlDocument plikXML = new XmlDocument();

            var elementGlowny = plikXML.CreateElement("xml");
            foreach(var kontakt in lista){
                
                var elementKontakt = plikXML.CreateElement("kontakt");
                
                var atrybutIP = plikXML.CreateAttribute("ip") ;
                atrybutIP.InnerText = kontakt.IP.ToString();
                elementKontakt.Attributes.Append(atrybutIP);

                var atrybutNazwa = plikXML.CreateAttribute("nazwa");
                atrybutNazwa.InnerText = kontakt.Nazwa;
                elementKontakt.Attributes.Append(atrybutNazwa);
                
                elementGlowny.AppendChild(elementKontakt);                
            }

            plikXML.AppendChild(elementGlowny);
            plikXML.Save(sciezkaPliku);        
        }
    }
}
