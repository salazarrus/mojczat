using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MojCzat.komunikacja
{
    class Buforownia
    {
        /// <summary>
        /// Ile bajtow mozna przeslac w jednej wiadomosci
        /// </summary>
        public const int RozmiarBufora = 1024;
        
        /// <summary>
        /// Do kazdego kontaktu jest przypisany bufor na przesylane przez niego wiadomosci
        /// </summary>
        Dictionary<string, byte[]> bufory = new Dictionary<string, byte[]>();

        public byte[] this[string id] {
            get { 
                if (!bufory.ContainsKey(id))
                {
                    bufory.Add(id, new byte[Buforownia.RozmiarBufora]);
                }
                return bufory[id];
            }
        }

        public void Usun(String idUzytkownika)
        {
            if (bufory.ContainsKey(idUzytkownika)) { bufory.Remove(idUzytkownika); }
        }
    }
}
