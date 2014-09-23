
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace MojCzat.komunikacja
{
    public class StrumienSieciowy
    {
        public String ID { get; protected set; }
        protected Stream strumien;

        public StrumienSieciowy(NetworkStream strumien, string idStrumienia) : this((Stream)strumien, idStrumienia)
        { }

        public StrumienSieciowy(Stream strumien, string idStrumienia) 
        {
            ID = idStrumienia;
            this.strumien = strumien;
        }

        public IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return strumien.BeginRead(buffer, offset, count, callback, state);
        }
        public IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return strumien.BeginWrite(buffer, offset, count, callback, state);
        }
        public int EndRead(IAsyncResult asyncResult)
        { 
            return strumien.EndRead(asyncResult);
        }
        public void EndWrite(IAsyncResult asyncResult)
        {
            strumien.EndWrite(asyncResult);
        }
        public void Close()
        {
            strumien.Close();    
        }

    }

    public class StrumienSieciowySsl: StrumienSieciowy 
    {
        public StrumienSieciowySsl(SslStream strumien, string idStrumienia):base(strumien, idStrumienia){ }

        public StrumienSieciowySsl(Stream strumien, string idStrumienia) : base(strumien, idStrumienia) { }
    }

}
