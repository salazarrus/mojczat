using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MojCzat.uzytki
{
    /// <summary>
    /// Klasa pomocnicza dla obslugi XML
    /// </summary>
    class Xml
    {
        /// <summary>
        /// Wczytaj atrybut
        /// </summary>
        /// <param name="wezel">skad</param>
        /// <param name="atrybut">jaki atrybut</param>
        /// <returns></returns>
        public static string DajAtrybut(XmlNode wezel, string atrybut)
        {
            if (wezel == null || atrybut == null || wezel.Attributes[atrybut] == null)
            { return string.Empty; }

            return wezel.Attributes[atrybut].InnerText;
        }

        /// <summary>
        /// Dodaj atrybut do wezla
        /// </summary>
        /// <param name="dokument">jaki dokument</param>
        /// <param name="element">ktory element</param>
        /// <param name="atrybut">jaki atrybut</param>
        /// <param name="wartosc">wartosc atrybutu</param>
        public static void DodajAtrybut(XmlDocument dokument, XmlElement element, string atrybut, string wartosc)
        {
            var nowyAtrybut = dokument.CreateAttribute(atrybut);
            nowyAtrybut.InnerText = wartosc;
            element.Attributes.Append(nowyAtrybut);
        }

    }
}
