using MojCzat.uzytki;
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

        /// <summary>
        /// Dostepny/niedostepny
        /// </summary>
        public String StatusTekst
        {
            get
            {
                return Polaczony ? "Dostępny" : "Niedostępny";
            }
        }

        /// <summary>
        /// Nazwa wyswietlana
        /// </summary>
        public string Nazwa { get; set; }

        /// <summary>
        /// Opis do statusu
        /// </summary>
        public string Opis { get; set; }

        /// <summary>
        /// Wczytaj liste kontaktow z pliku XML
        /// </summary>
        /// <param name="sciezkaPliku">sciezka do pliku</param>
        /// <returns></returns>
        public static List<Kontakt> WczytajListeKontaktow(string sciezkaPliku)
        {
            List<Kontakt> listaWynikowa = new List<Kontakt>();
            XmlDocument plikXML = new XmlDocument();
            try
            {
                plikXML.Load(sciezkaPliku);
                foreach (XmlNode wezel in plikXML.DocumentElement.ChildNodes)
                {
                    string ip = Xml.DajAtrybut(wezel, "ip");
                    string id = ip;
                    string nazwa = Xml.DajAtrybut(wezel, "nazwa");
                    listaWynikowa.Add(new Kontakt() { ID = id, IP = IPAddress.Parse(ip), Nazwa = nazwa, Polaczony = false });
                }
            }
            catch { return listaWynikowa; }

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

            var elementGlowny = plikXML.CreateElement("kontakty");
            foreach (var kontakt in lista)
            {

                var elementKontakt = plikXML.CreateElement("kontakt");

                Xml.DodajAtrybut(plikXML, elementKontakt, "ip", kontakt.IP.ToString());
                Xml.DodajAtrybut(plikXML, elementKontakt, "nazwa", kontakt.Nazwa);

                elementGlowny.AppendChild(elementKontakt);
            }

            plikXML.AppendChild(elementGlowny);
            plikXML.Save(sciezkaPliku);
        }
    }
}
