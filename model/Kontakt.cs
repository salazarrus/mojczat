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


        public static List<Kontakt> WczytajListeKontaktow(string plik) {
            List<Kontakt> listaWynikowa = new List<Kontakt>();
            XmlDocument listaPlik = new XmlDocument();
            listaPlik.Load(plik);
            foreach (XmlNode node in listaPlik.DocumentElement.ChildNodes)
            {
                string id = node.Attributes["id"].InnerText;
                string ip = node.Attributes["ip"].InnerText;
                int port = int.Parse(node.Attributes["port"].InnerText);
                listaWynikowa.Add(new Kontakt() { ID = id, PunktKontaktu = 
                    new IPEndPoint(IPAddress.Parse(ip), port) });
            }
            
            return listaWynikowa;        
        }

        public static void ZapiszListeKontaktow(List<Kontakt> lista, string plik) {
            XmlDocument listaPlik = new XmlDocument();

            var head = listaPlik.CreateElement("xml");
            foreach(var kontakt in lista){
                
                var element = listaPlik.CreateElement("kontakt");
                var attrId = listaPlik.CreateAttribute("id") ;
                attrId.InnerText = kontakt.ID;
                element.Attributes.Append(attrId);

                var attrIp = listaPlik.CreateAttribute("ip") ;
                attrIp.InnerText = kontakt.PunktKontaktu.Address.ToString();
                element.Attributes.Append(attrIp);
                
                var attrPort = listaPlik.CreateAttribute("port") ;
                attrPort.InnerText = kontakt.PunktKontaktu.Port.ToString();
                element.Attributes.Append(attrPort);

                head.AppendChild(element);
                
            }
            listaPlik.AppendChild(head);
            listaPlik.Save(plik);        
        }
    }
}
