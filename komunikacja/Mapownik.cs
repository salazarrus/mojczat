using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MojCzat.komunikacja
{
    /// <summary>
    /// Obiekt mapujacy identyfikator uzytkownika do adresu IP i odwrotnie
    /// </summary>
    class Mapownik
    {
        // Mapowanie Identyfikatora rozmowcy do adresu IP
        Dictionary<string, IPAddress> ID_IP;

        /// <summary>
        /// Odwrotnosc mapy ID_IP
        /// </summary>
        Dictionary<IPAddress, string> IP_ID;

        /// <summary>
        /// Znajdz adres IP tego uzytkownika
        /// </summary>
        /// <param name="id">Identyfikator uzytkownika</param>
        /// <returns>adres IP</returns>
        public IPAddress this[string id] { get { return ID_IP[id]; } }

        /// <summary>
        /// Powiedz kto znajduje sie pod tym adresem IP
        /// </summary>
        /// <param name="ip">adres IP</param>
        /// <returns>Identyfikator uzytkownika</returns>
        public string this[IPAddress ip] { get { return IP_ID[ip]; } }

        /// <summary>
        /// Daj liste wszystkich uzytkownikow, ktorych lokalizacje znasz
        /// </summary>       
        public List<string> WszystkieId { get { return ID_IP.Keys.ToList(); } }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="ID_IP">mapowanie z identyfikatora uzytkownika do adresu IP</param>
        public Mapownik(Dictionary<string, IPAddress> ID_IP)
        {
            this.ID_IP = ID_IP;
            this.IP_ID = new Dictionary<IPAddress, string>();
            foreach (var i in ID_IP)
            { IP_ID.Add(i.Value, i.Key); }
        }

        /// <summary>
        /// Obsluguj nowego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika </param>
        /// <param name="ip">adres IP</param>
        public void Dodaj(string idUzytkownika, IPAddress ip)
        {
            IP_ID.Add(ip, idUzytkownika);
            ID_IP.Add(idUzytkownika, ip);
        }

        /// <summary>
        /// Nie obsluguj juz tego uzytkownika
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika </param>
        public void Usun(string idUzytkownika)
        {
            IP_ID.Remove(ID_IP[idUzytkownika]);
            ID_IP.Remove(idUzytkownika);
        }

        /// <summary>
        /// Czy obslugujesz tego uzytkownika?
        /// </summary>
        /// <param name="idUzytkownika">Identyfikator uzytkownika </param>
        /// <returns></returns>
        public bool CzyZnasz(String idUzytkownika)
        { return ID_IP.ContainsKey(idUzytkownika); }

        /// <summary>
        /// Czy obslugujesz uzytkownika o tym adresie IP?
        /// </summary>
        /// <param name="ip">adres IP</param>
        /// <returns></returns>
        public bool CzyZnasz(IPAddress ip)
        { return IP_ID.ContainsKey(ip); }
    }
}
