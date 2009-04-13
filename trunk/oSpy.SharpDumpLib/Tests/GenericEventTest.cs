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
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture ()]
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
            Assert.That (ev, Is.Not.Null & Is.TypeOf (typeof (Event)));

            Assert.That (ev.Id, Is.EqualTo (1));
            Assert.That (ev.Type, Is.EqualTo (EventType.Error));
            Assert.That (ev.ProcessId, Is.EqualTo (2684));
            Assert.That (ev.ProcessName, Is.EqualTo ("msnmsgr.exe"));
            Assert.That (ev.ThreadId, Is.EqualTo (1128));
            Assert.That (ev.Timestamp, Is.EqualTo (DateTime.FromFileTimeUtc (128837553502326832)));

            string expected_body = CanonicalizeEventBodyXml ("<data>" + error_event_body + "</data>");
            string actual_body = CanonicalizeEventBodyXml (ev.Data);
            Assert.That (actual_body, Is.EqualTo (expected_body));
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
