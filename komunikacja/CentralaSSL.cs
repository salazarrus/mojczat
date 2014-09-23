#define TRACE
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
    /// <summary>
    /// Obiekt odpowiedzialny za otwieranie i zamykanie polaczen SSL
    /// </summary>
    class CentralaSSL:Centrala
    {

        // Certyfikat serwera - pozwala laczyc sie przez SSL/TLS
        X509Certificate2 certyfikat;

        protected override int Port { get { return 5443; } }

        public CentralaSSL(X509Certificate2 certyfikat):base()
        { this.certyfikat = certyfikat; }
         
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

        // Spradzamy waznosc certyfikatu 
        bool sprawdzCertyfikat(object sender, X509Certificate certyfikat,
            X509Chain lancuch, SslPolicyErrors bledy)
        {            
            /*  w rzeczywistosci powinno byc mniej wiecej tak:
                
                 return bledy == SslPolicyErrors.None;
                ale nie mamy testowych certyfikatow
                podpisanych przez instytucje zaufane przez Microsoft
            */
            return true;
        }
    }
}
