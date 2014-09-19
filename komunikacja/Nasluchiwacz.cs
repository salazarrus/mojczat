#define TRACE


using MojCzat.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MojCzat.komunikacja
{
    class Nasluchiwacz
    {

        /// <summary>
        /// Na jakim porcie nasluchujemy wiadomosci
        /// </summary>
        int port;

        /// <summary>
        /// obiekt nasluchujacy nadchodzacych polaczen
        /// </summary>
        TcpListener serwer;

        public Nasluchiwacz(int port) {
            this.port = port;
        }

        public event NowyKlient NowyKlient;

        /// <summary>
        /// Oczekuj nadchodzacych polaczen
        /// </summary>
        public void Start()
        {
            try
            {
                serwer = new TcpListener(IPAddress.Any, port); // stworz serwer
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

        public void Stop()
        {
            // zatrzymaj nasluch
            if (serwer != null) { serwer.Stop(); }
        }
    }
}
