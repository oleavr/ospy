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
using System.Xml;
using NUnit.Framework;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture()]
    public class GenericEventTest
    {
        private const string error_event_xml =
             "<event id=\"1\" type=\"Error\" timestamp=\"128837553502326832\" processName=\"msnmsgr.exe\" processId=\"2684\" threadId=\"1128\">\n"
            + error_event_body
            +"</event>";
        private const string error_event_body =
            "    <message>signature 'RTCDebug' specified for function not found: No matches found</message>\n";

        [Test ()]
        public void FromXml ()
        {
            Event ev = EventFactory.CreateFromXml (error_event_xml);
            Assert.IsNotNull (ev);
            Assert.AreEqual (typeof (Event), ev.GetType ());

            Assert.AreEqual (1, ev.Id);
            Assert.AreEqual (EventType.Error, ev.Type);
            Assert.AreEqual (2684, ev.ProcessId);
            Assert.AreEqual ("msnmsgr.exe", ev.ProcessName);
            Assert.AreEqual (1128, ev.ThreadId);
            Assert.AreEqual (DateTime.FromFileTimeUtc (128837553502326832), ev.Timestamp);

            string expected_body = CanonicalizeEventBodyXml ("<data>" + error_event_body + "</data>");
            string actual_body = CanonicalizeEventBodyXml (ev.Data);
            Assert.AreEqual (expected_body, actual_body);
        }

        private string CanonicalizeEventBodyXml (string xml)
        {
            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (xml);
            doc.Normalize ();
            StringBuilder sb = new StringBuilder ();
            XmlWriter writer = XmlTextWriter.Create (sb);
            doc.WriteTo (writer);
            writer.Flush ();
            writer.Close ();
            return sb.ToString ();
        }
    }
}
