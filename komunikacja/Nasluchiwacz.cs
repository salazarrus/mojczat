#define TRACE


using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MojCzat.komunikacja
{
    public delegate void NowyKlient(TcpClient polaczenie);

    class Nasluchiwacz
    {
        // Na jakim porcie nasluchujemy wiadomosci
        int port;

        // obiekt nasluchujacy nadchodzacych polaczen
        TcpListener serwer;

        public Nasluchiwacz(int port) { this.port = port; }

        public event NowyKlient NowyKlient;

        // Oczekuj nadchodzacych polaczen
        public void Start()
        {
            try
            {
                int portMoj; 
                int.TryParse(ConfigurationManager.AppSettings["portMoj"], out portMoj);
                serwer = new TcpListener(IPAddress.Any, portMoj); // stworz serwer
                serwer.Start(); //uruchom serwer

                while (true) // zapetlamy
                {
                    // czekaj na przychodzace polaczenia
                    TcpClient polaczenie = serwer.AcceptTcpClient();
                    if (NowyKlient != null) { NowyKlient(polaczenie); }
                }
            }
            catch (Exception ex) { Trace.TraceInformation("[Start]" + ex.ToString()); } // program zostal zamkniety
            finally { Stop(); }
        }

        // zatrzymaj nasluch
        public void Stop() { if (serwer != null) { serwer.Stop(); } }
    }
}
