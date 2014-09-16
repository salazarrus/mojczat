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
        /// Adres IP i port uzytkownika
        /// </summary>
        public IPEndPoint PunktKontaktu { get; set; }
 
        /// <summary>
        /// Dostepnosc czatu z tym uzytkownikiem
        /// </summary>
        public string Status { get; set; }
        
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
                    string id = node.Attributes["id"].InnerText;
                    string ip = node.Attributes["ip"].InnerText;
                    int port = int.Parse(node.Attributes["port"].InnerText);
                    listaWynikowa.Add(new Kontakt() { ID = id, PunktKontaktu = 
                        new IPEndPoint(IPAddress.Parse(ip), port) , Status="Niedostepny" });
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
                var atrybutID = plikXML.CreateAttribute("id") ;
                atrybutID.InnerText = kontakt.ID;
                elementKontakt.Attributes.Append(atrybutID);

                var atrybutIP = plikXML.CreateAttribute("ip") ;
                atrybutIP.InnerText = kontakt.PunktKontaktu.Address.ToString();
                elementKontakt.Attributes.Append(atrybutIP);
                
                var atrybutPort = plikXML.CreateAttribute("port") ;
                atrybutPort.InnerText = kontakt.PunktKontaktu.Port.ToString();
                elementKontakt.Attributes.Append(atrybutPort);

                elementGlowny.AppendChild(elementKontakt);                
            }

            plikXML.AppendChild(elementGlowny);
            plikXML.Save(sciezkaPliku);        
        }
    }
}
