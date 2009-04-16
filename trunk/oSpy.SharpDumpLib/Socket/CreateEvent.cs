//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
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
        private AddressFamily m_addressFamily;
        private SocketType m_socketType;
        private ProtocolType m_protocolType;
        private uint m_result;

        public AddressFamily AddressFamily
        {
            get
            {
                return m_addressFamily;
            }
        }

        public SocketType SocketType
        {
            get
            {
                return m_socketType;
            }
        }

        public ProtocolType ProtocolType
        {
            get
            {
                return m_protocolType;
            }
        }

        public uint Result
        {
            get
            {
                return m_result;
            }
        }

        public CreateEvent(EventInformation eventInformation, AddressFamily addressFamily, SocketType socketType,
                           ProtocolType protocolType, uint result)
            : base(eventInformation)
        {
            m_addressFamily = addressFamily;
            m_socketType = socketType;
            m_protocolType = protocolType;
            m_result = result;
        }
    }

    [FunctionCallEventFactory("socket")]
    public class CreateEventFactory : ISpecificEventFactory
    {
        public Event CreateEvent(EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            FunctionCallDataElement el = new FunctionCallDataElement(eventData);

            string familyStr = el.FirstArgument;
            AddressFamily family = AddressFamily.Unknown;
            if (familyStr == "AF_INET")
                family = AddressFamily.InterNetwork;
            else if (familyStr == "AF_INET6")
                family = AddressFamily.InterNetworkV6;

            string typeStr = el.SecondArgument;
            SocketType type = SocketType.Unknown;
            if (typeStr == "SOCK_STREAM")
                type = SocketType.Stream;
            else if (typeStr == "SOCK_DGRAM")
                type = SocketType.Dgram;

            string protocolStr = el.ThirdArgument;
            ProtocolType protocol = ProtocolType.Unknown;
            if (protocolStr == "IPPROTO_IP")
                protocol = ProtocolType.IP;

            uint result = el.ReturnValueAsUInt;

            return new CreateEvent(eventInformation, family, type, protocol, result);
        }
    }
}
