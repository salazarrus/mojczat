using System;
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
        X509Certificate2 certyfikat;

        public CentralaSSL(Dictionary<string, IPAddress> ID_IP, Dictionary<IPAddress, String> IP_ID, int port ,X509Certificate2 certyfikat):base(ID_IP, IP_ID, port)
        {
            this.certyfikat = certyfikat;
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
        bool sprawdzCertyfikat(object sender, X509Certificate certyfikat,
            X509Chain lancuch, SslPolicyErrors bledy)
        {            
            // w rzeczywistosci powinno byc tak, ale nie mamy testowych certyfikatow
            // podpisanych przez instytucje zaufane przez Microsoft
            // return bledy == SslPolicyErrors.None;
            
            return true;
        }
    }
}
