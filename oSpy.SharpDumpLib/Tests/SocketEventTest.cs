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
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture ()]
    public class SocketEventTest
    {
        [Test ()]
        public void CreateEvent ()
        {
            const string xml =
                 "<event id=\"83\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553521454336\" type=\"FunctionCall\">\n"
                +"    <name>\n"
                +"        WS2_32.dll::socket\n"
                +"    </name>\n"
                +"    <arguments direction=\"in\">\n"
                +"        <argument name=\"af\">\n"
                +"            <value subType=\"SockAddrFamily\" type=\"Enum\" value=\"AF_INET\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"type\">\n"
                +"            <value subType=\"SockType\" type=\"Enum\" value=\"SOCK_STREAM\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"protocol\">\n"
                +"            <value subType=\"AF_INET_Protocol\" type=\"Dynamic\" value=\"IPPROTO_IP\"/>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <returnValue>\n"
                +"        <value type=\"UInt32\" value=\"0x8ac\"/>\n"
                +"    </returnValue>\n"
                +"    <lastError value=\"0\"/>\n"
                +"</event>";
            Socket.CreateEvent ev = EventFactory.CreateFromXml (xml) as Socket.CreateEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.AddressFamily, Is.EqualTo (System.Net.Sockets.AddressFamily.InterNetwork));
            Assert.That (ev.SocketType, Is.EqualTo (System.Net.Sockets.SocketType.Stream));
            Assert.That (ev.ProtocolType, Is.EqualTo (System.Net.Sockets.ProtocolType.IP));
            Assert.That (ev.Result, Is.EqualTo (0x8ac));
        }

        [Test ()]
        public void CloseEvent ()
        {
            const string xml =
                 "<event id=\"140\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553527062400\" type=\"FunctionCall\">\n"
                +"    <name>\n"
                +"        WS2_32.dll::closesocket\n"
                +"    </name>\n"
                +"    <arguments direction=\"in\">\n"
                +"        <argument name=\"s\">\n"
                +"            <value type=\"UInt32\" value=\"0x8ac\"/>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <returnValue>\n"
                +"        <value type=\"Int32\" value=\"0\"/>\n"
                +"    </returnValue>\n"
                +"    <lastError value=\"0\"/>\n"
                +"</event>\n";
            Socket.CloseEvent ev = EventFactory.CreateFromXml (xml) as Socket.CloseEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Result, Is.EqualTo (0));
        }

        [Test ()]
        public void ConnectEvent ()
        {
            const string xml =
                 "<event id=\"84\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553521454336\" type=\"FunctionCall\">\n"
                +"    <name>\n"
                +"        WS2_32.dll::connect\n"
                +"    </name>\n"
                +"    <arguments direction=\"in\">\n"
                +"        <argument name=\"s\">\n"
                +"            <value type=\"UInt32\" value=\"0x8ac\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"name\">\n"
                +"            <value type=\"Ipv4SockaddrPtr\" value=\"0x0006FCEC\">\n"
                +"                <value subType=\"Ipv4Sockaddr\" type=\"Struct\">\n"
                +"                    <field name=\"sin_family\">\n"
                +"                        <value subType=\"SockAddrFamily\" type=\"Enum\" value=\"AF_INET\"/>\n"
                +"                    </field>\n"
                +"                    <field name=\"sin_port\">\n"
                +"                        <value type=\"UInt16\" value=\"1863\"/>\n"
                +"                    </field>\n"
                +"                    <field name=\"sin_addr\">\n"
                +"                        <value type=\"Ipv4InAddr\" value=\"65.54.239.20\"/>\n"
                +"                    </field>\n"
                +"                </value>\n"
                +"            </value>\n"
                +"        </argument>\n"
                +"        <argument name=\"namelen\">\n"
                +"            <value type=\"Int32\" value=\"16\"/>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <returnValue>\n"
                +"        <value type=\"Int32\" value=\"-1\"/>\n"
                +"    </returnValue>\n"
                +"    <lastError value=\"10035\"/>\n"
                +"</event>\n";
            Socket.ConnectEvent ev = EventFactory.CreateFromXml (xml) as Socket.ConnectEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            System.Net.IPEndPoint expectedEndpoint = new System.Net.IPEndPoint (System.Net.IPAddress.Parse ("65.54.239.20"), 1863);
            Assert.That (ev.RemoteEndPoint, Is.EqualTo (expectedEndpoint));
            Assert.That (ev.Result, Is.EqualTo (Socket.ConnectResult.WouldBlock));
        }

        [Test ()]
        public void SendEvent ()
        {
            const string xml =
                 "<event id=\"96\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553523557360\" type=\"FunctionCall\">\n"
                +"    <name>\n"
                +"        WS2_32.dll::send\n"
                +"    </name>\n"
                +"    <arguments direction=\"in\">\n"
                +"        <argument name=\"s\">\n"
                +"            <value type=\"UInt32\" value=\"0x8ac\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"buf\">\n"
                +"            <value type=\"Pointer\" value=\"0x020DCD38\">\n"
                +"                <value size=\"26\" type=\"ByteArray\">\n"
                +"                    VkVSIDEgTVNOUDE4IE1TTlAxNyBDVlIwDQo=\n"
                +"                </value>\n"
                +"            </value>\n"
                +"        </argument>\n"
                +"        <argument name=\"len\">\n"
                +"            <value type=\"Int32\" value=\"26\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"flags\">\n"
                +"            <value type=\"Int32\" value=\"0\"/>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <returnValue>\n"
                +"        <value type=\"Int32\" value=\"26\"/>\n"
                +"    </returnValue>\n"
                +"    <lastError value=\"0\"/>\n"
                +"</event>\n";
            Socket.SendEvent ev = EventFactory.CreateFromXml (xml) as Socket.SendEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Buffer, Is.EqualTo (Encoding.UTF8.GetBytes ("VER 1 MSNP18 MSNP17 CVR0\r\n")));
            Assert.That (ev.Flags, Is.EqualTo (0));
            Assert.That (ev.Result, Is.EqualTo (26));
        }

        [Test ()]
        public void ReceiveEvent ()
        {
            const string xml =
                 "<event id=\"130\" processId=\"2684\" processName=\"msnmsgr.exe\" threadId=\"544\" timestamp=\"128837553525259808\" type=\"FunctionCall\">\n"
                +"    <name>\n"
                +"        WSOCK32.dll::recv\n"
                +"    </name>\n"
                +"    <arguments direction=\"in\">\n"
                +"        <argument name=\"s\">\n"
                +"            <value type=\"UInt32\" value=\"0x8ac\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"buf\">\n"
                +"            <value type=\"Pointer\" value=\"0x00C08230\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"len\">\n"
                +"            <value type=\"Int32\" value=\"512\"/>\n"
                +"        </argument>\n"
                +"        <argument name=\"flags\">\n"
                +"            <value type=\"Int32\" value=\"0\"/>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <arguments direction=\"out\">\n"
                +"        <argument name=\"buf\">\n"
                +"            <value type=\"Pointer\" value=\"0x00C08230\">\n"
                +"                <value size=\"14\" type=\"ByteArray\">\n"
                +"                    VkVSIDEgTVNOUDE4DQo=\n"
                +"                </value>\n"
                +"            </value>\n"
                +"        </argument>\n"
                +"    </arguments>\n"
                +"    <returnValue>\n"
                +"        <value type=\"Int32\" value=\"14\"/>\n"
                +"    </returnValue>\n"
                +"    <lastError value=\"0\"/>\n"
                +"</event>\n";
            Socket.ReceiveEvent ev = EventFactory.CreateFromXml (xml) as Socket.ReceiveEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Buffer, Is.EqualTo (Encoding.UTF8.GetBytes ("VER 1 MSNP18\r\n")));
            Assert.That (ev.BufferSize, Is.EqualTo (512));
            Assert.That (ev.Flags, Is.EqualTo (0));
            Assert.That (ev.Result, Is.EqualTo (14));
        }
    }
}
