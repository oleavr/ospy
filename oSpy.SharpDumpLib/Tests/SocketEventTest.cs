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
        public void CreateEvent()
        {
            Socket.CreateEvent ev = EventFactory.CreateFromXml (TestEventXml.E083_CreateSocket) as Socket.CreateEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.AddressFamily, Is.EqualTo (System.Net.Sockets.AddressFamily.InterNetwork));
            Assert.That (ev.SocketType, Is.EqualTo (System.Net.Sockets.SocketType.Stream));
            Assert.That (ev.ProtocolType, Is.EqualTo (System.Net.Sockets.ProtocolType.IP));
            Assert.That (ev.Result, Is.EqualTo (0x8ac));
        }

        [Test ()]
        public void CloseEvent ()
        {
            Socket.CloseEvent ev = EventFactory.CreateFromXml (TestEventXml.E140_CloseSocket) as Socket.CloseEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Result, Is.EqualTo (0));
        }

        [Test ()]
        public void ConnectEvent ()
        {
            Socket.ConnectEvent ev = EventFactory.CreateFromXml (TestEventXml.E084_Connect) as Socket.ConnectEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            System.Net.IPEndPoint expectedEndpoint = new System.Net.IPEndPoint (System.Net.IPAddress.Parse ("65.54.239.20"), 1863);
            Assert.That (ev.RemoteEndPoint, Is.EqualTo (expectedEndpoint));
            Assert.That (ev.Result, Is.EqualTo (Socket.ConnectResult.WouldBlock));
        }

        [Test ()]
        public void SendEvent ()
        {
            Socket.SendEvent ev = EventFactory.CreateFromXml (TestEventXml.E096_Send) as Socket.SendEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Buffer, Is.EqualTo (Encoding.UTF8.GetBytes ("VER 1 MSNP18 MSNP17 CVR0\r\n")));
            Assert.That (ev.Flags, Is.EqualTo (0));
            Assert.That (ev.Result, Is.EqualTo (26));
        }

        [Test ()]
        public void ReceiveEvent ()
        {
            Socket.ReceiveEvent ev = EventFactory.CreateFromXml (TestEventXml.E130_Receive) as Socket.ReceiveEvent;
            Assert.That (ev, Is.Not.Null);
            Assert.That (ev.Socket, Is.EqualTo (0x8ac));
            Assert.That (ev.Buffer, Is.EqualTo (Encoding.UTF8.GetBytes ("VER 1 MSNP18\r\n")));
            Assert.That (ev.BufferSize, Is.EqualTo (512));
            Assert.That (ev.Flags, Is.EqualTo (0));
            Assert.That (ev.Result, Is.EqualTo (14));
        }
    }
}
