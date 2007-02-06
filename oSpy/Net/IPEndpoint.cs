using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Net
{
    [Serializable()]
    public class IPEndpoint
    {
        protected string address;
        public string Address
        {
            get
            {
                return address;
            }
        }

        protected int port;
        public int Port
        {
            get
            {
                return port;
            }
        }

        public IPEndpoint(string address, int port)
        {
            this.address = LookupAddress(address);
            this.port = port;
        }

        public override string ToString()
        {
            if (address.Length > 0 && port > 0)
            {
                return String.Format("{0}:{1}", address, port);
            }
            else
            {
                return "";
            }
        }

        protected static string LookupAddress(string addr)
        {
            if (addr == "169.254.2.1")
                return "PDA";
            else if (addr == "169.254.2.2")
                return "HOST";
            else
                return addr;
        }
    }
}
