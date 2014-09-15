﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MojCzat.komunikacja
{
    class CentralaSSL:Centrala
    {
        /// <summary>
        /// Certyfikat serwera - pozwala laczyc sie przez SSL/TLS
        /// </summary>
        X509Certificate certyfikat;


        public CentralaSSL()
        {
            certyfikat = new X509Certificate2("cert\\cert1.pfx", "cert1pwd");
        }

         
        protected override Stream dajStrumienJakoKlient(TcpClient polaczenie)
        {
            var strumien = new SslStream(polaczenie.GetStream(), true, new
                   RemoteCertificateValidationCallback(sprawdzCertyfikat));
            string host = ((IPEndPoint)polaczenie.Client.RemoteEndPoint).Address.ToString();
            strumien.AuthenticateAsClient(host);

            return strumien;
        }

        protected override Stream dajStrumienJakoSerwer(TcpClient polaczenie)
        {
            var strumien = new SslStream(polaczenie.GetStream(), false);
            strumien.AuthenticateAsServer(certyfikat, false, SslProtocols.Tls, false);

            return strumien;
        }

        /// <summary>
        /// Spradzamy waznosc certyfikatu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        bool sprawdzCertyfikat(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}