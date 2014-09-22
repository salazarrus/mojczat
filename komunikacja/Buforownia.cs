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
        public int RozmiarBufora { get; private set; }


        // bufory na przychodzace dane
        Dictionary<string, byte[]> bufory = new Dictionary<string, byte[]>();

        public byte[] this[string id]
        {
            get
            {
                if (!bufory.ContainsKey(id))
                {
                    bufory.Add(id, new byte[RozmiarBufora]);
                }
                return bufory[id];
            }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="rozmiarBufora">ile bajtow ma bufor</param>
        public Buforownia(int rozmiarBufora)
        { RozmiarBufora = rozmiarBufora; }

        /// <summary>
        /// Nie potrzebujemy juz tego bufora
        /// </summary>
        /// <param name="idBufora">Identyfikator bufora</param>
        public void Usun(String idBufora)
        {
            if (bufory.ContainsKey(idBufora)) { bufory.Remove(idBufora); }
        }
    }
}
