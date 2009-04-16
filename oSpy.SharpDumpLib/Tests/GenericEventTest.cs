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
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture()]
    public class GenericEventTest
    {
        [Test()]
        public void FromXml()
        {
            Event ev = EventFactory.CreateFromXml(TestEventXml.E001_Error);
            Assert.That(ev, Is.Not.Null & Is.TypeOf(typeof(Event)));

            Assert.That(ev.Id, Is.EqualTo(1));
            Assert.That(ev.Type, Is.EqualTo(EventType.Error));
            Assert.That(ev.ProcessId, Is.EqualTo(2684));
            Assert.That(ev.ProcessName, Is.EqualTo("msnmsgr.exe"));
            Assert.That(ev.ThreadId, Is.EqualTo(1128));
            Assert.That(ev.Timestamp, Is.EqualTo(DateTime.FromFileTimeUtc(128837553502326832)));

            string expectedBody = XmlString.Canonicalize(TestEventXml.E001_Error);
            string actualBody = ev.RawData;
            Assert.That(actualBody, Is.EqualTo(expectedBody));
        }
    }
}
