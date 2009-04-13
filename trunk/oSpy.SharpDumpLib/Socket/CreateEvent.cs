//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Net.Sockets;

namespace oSpy.SharpDumpLib.Socket
{
    public class CreateEvent : Event
    {
        private AddressFamily address_family;
        public AddressFamily AddressFamily {
            get { return address_family; }
        }

        private SocketType socket_type;
        public SocketType SocketType {
            get { return socket_type; }
        }

        private ProtocolType protocol_type;
        public ProtocolType ProtocolType {
            get { return protocol_type; }
        }

        private uint result;
        public uint Result {
            get { return result; }
        }

        public CreateEvent (EventInformation eventInformation,
                            AddressFamily af, SocketType socketType,
                            ProtocolType protocolType, uint result)
            : base (eventInformation)
        {
            this.address_family = af;
            this.socket_type = socketType;
            this.protocol_type = protocolType;
            this.result = result;
        }
    }

    [FunctionCallEventFactory ("socket")]
    public class CreateEventFactory : SpecificEventFactory
    {
        public Event CreateEvent (EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            FunctionCallDataElement el = new FunctionCallDataElement (eventData);

            string family_str = el.FirstArgument;
            AddressFamily af = AddressFamily.Unknown;
            if (family_str == "AF_INET")
                af = AddressFamily.InterNetwork;
            else if (family_str == "AF_INET6")
                af = AddressFamily.InterNetworkV6;

            string socket_type_str = el.SecondArgument;
            SocketType socket_type = SocketType.Unknown;
            if (socket_type_str == "SOCK_STREAM")
                socket_type = SocketType.Stream;
            else if (socket_type_str == "SOCK_DGRAM")
                socket_type = SocketType.Dgram;

            string protocol_type_str = el.ThirdArgument;
            ProtocolType protocol_type = ProtocolType.Unknown;
            if (protocol_type_str == "IPPROTO_IP")
                protocol_type = ProtocolType.IP;

            uint result = el.ReturnValueAsUInt;

            return new CreateEvent (eventInformation, af, socket_type, protocol_type, result);
        }
    }
}
