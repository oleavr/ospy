//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
