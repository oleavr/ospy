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
using System.Net;
using System.Xml;

namespace oSpy.SharpDumpLib.Socket
{
    public enum ConnectResult
    {
        Success,
        WouldBlock,
        Error
    }

    public class ConnectEvent : Event
    {
        private uint m_socket;
        private EndPoint m_remoteEndPoint;
        private ConnectResult m_result;

        public uint Socket
        {
            get
            {
                return m_socket;
            }
        }

        public EndPoint RemoteEndPoint
        {
            get
            {
                return m_remoteEndPoint;
            }
        }

        public ConnectResult Result
        {
            get
            {
                return m_result;
            }
        }

        public ConnectEvent(EventInformation eventInformation, uint socket, EndPoint remoteEP, ConnectResult result)
            : base(eventInformation)
        {
            m_socket = socket;
            m_remoteEndPoint = remoteEP;
            m_result = result;
        }
    }

    [FunctionCallEventFactory("connect")]
    public class ConnectEventFactory : ISpecificEventFactory
    {
        public Event CreateEvent(EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            FunctionCallDataElement el = new FunctionCallDataElement(eventData);

            uint socket = el.GetSimpleArgumentValueAsUInt(1);

            EndPoint ep = null;
            XmlNode saNode = eventData.SelectSingleNode("/event/arguments[@direction='in']/argument[2]/value/value");
            string family = saNode.SelectSingleNode("field[@name='sin_family']/value/@value").Value;
            if (family == "AF_INET")
            {
                string addr = saNode.SelectSingleNode("field[@name='sin_addr']/value/@value").Value;
                int port = Convert.ToInt32(saNode.SelectSingleNode("field[@name='sin_port']/value/@value").Value);
                ep = new IPEndPoint(IPAddress.Parse(addr), port);
            }

            ConnectResult result;
            if (el.ReturnValueAsInt == 0)
            {
                result = ConnectResult.Success;
            }
            else
            {
                if (el.LastError == 10035)
                    result = ConnectResult.WouldBlock;
                else
                    result = ConnectResult.Error;
            }

            return new ConnectEvent(eventInformation, socket, ep, result);
        }
    }
}
