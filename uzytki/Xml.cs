using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MojCzat.uzytki
{
    class Xml
    {
        public static string DajAtrybut(XmlNode wezel, string atrybut)
        {
            if (wezel == null || atrybut == null || wezel.Attributes[atrybut] == null)
            { return string.Empty; }

            return wezel.Attributes[atrybut].InnerText;
        }

        public static void DodajAtrybut(XmlDocument dokument, XmlElement element, string atrybut, string wartosc)
        {
            var nowyAtrybut = dokument.CreateAttribute(atrybut);
            nowyAtrybut.InnerText = wartosc;
            element.Attributes.Append(nowyAtrybut);
        }

    }
}
