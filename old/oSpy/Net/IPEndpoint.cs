//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

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
