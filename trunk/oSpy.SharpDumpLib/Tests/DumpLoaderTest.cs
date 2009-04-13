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
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture ()]
    public class DumpLoaderTest
    {
        [Test ()]
        public void LoadUncompressed ()
        {
            MemoryStream stream = new MemoryStream ();
            BinaryWriter bw = new BinaryWriter (stream, Encoding.UTF8);

            {
                byte[] magic = Encoding.ASCII.GetBytes ("oSpy");
                const uint version = 2;
                const uint is_compressed = 0;
                const uint num_events = 3;

                bw.Write (magic);
                bw.Write (version);
                bw.Write (is_compressed);
                bw.Write (num_events);

                bw.Write (Encoding.UTF8.GetBytes ("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><events>\n"));
                bw.Write (Encoding.UTF8.GetBytes (
                     "<event id=\"1\" type=\"Error\" timestamp=\"128837553502326832\" processName=\"msnmsgr.exe\" processId=\"2684\" threadId=\"1128\">\n"
                    +"    <message>\n"
                    +"        signature 'RTCDebug' specified for function not found: No matches found\n"
                    +"    </message>\n"
                    +"</event>"
                ));
                bw.Write (Encoding.UTF8.GetBytes (
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
                    +"</event>"
                ));
                bw.Write (Encoding.UTF8.GetBytes (
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
                    +"</event>\n"
                ));
                bw.Write (Encoding.UTF8.GetBytes ("</events>"));

                bw.Flush ();
            }

            stream.Flush ();
            stream.Seek (0, SeekOrigin.Begin);

            DumpLoader loader = new DumpLoader ();
            Dump dump = loader.Load (stream);
            Assert.That (dump.Events.Count, Is.EqualTo (3));
            Assert.That (dump.Events.Keys, Is.EquivalentTo (new uint[] { 1, 83, 140 }));
            Assert.That (dump.Events[1].Id, Is.EqualTo (1));
            Assert.That (dump.Events[83].Id, Is.EqualTo (83));
            Assert.That (dump.Events[140].Id, Is.EqualTo (140));
        }
    }
}
