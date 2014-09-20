using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    class Mapownik
    {
        /// <summary>
        /// Mapowanie Identyfikatora rozmowcy do punktu kontatku (adres IP,Port)
        /// </summary>
        Dictionary<string, IPAddress> ID_IP;

        /// <summary>
        /// Odwrotnosc mapy ID_IP
        /// </summary>
        Dictionary<IPAddress, string> IP_ID;

        public IPAddress this[string id] { get { return ID_IP[id]; } }
        public string this[IPAddress ip] { get { return IP_ID[ip]; } }

        public List<IPAddress> wszystkieIP { get { return IP_ID.Keys.ToList(); } }
        
        public List<string> WszystkieId { get { return ID_IP.Keys.ToList(); } }
        
        public Mapownik(Dictionary<string, IPAddress> mapa_ID_PunktKontaktu){
            this.ID_IP = mapa_ID_PunktKontaktu;
            this.IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in mapa_ID_PunktKontaktu)
            { IP_ID.Add(i.Value, i.Key); }
        }

        public void Dodaj(string id, IPAddress ip) {
            IP_ID.Add(ip, id);
            ID_IP.Add(id, ip);
        }

        public void Usun(string id) {
            IP_ID.Remove(ID_IP[id]);
            ID_IP.Remove(id);
        }

        public bool CzyZnasz(String id) {
            return ID_IP.ContainsKey(id);
        }
    }
}
