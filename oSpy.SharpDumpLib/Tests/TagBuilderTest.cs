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
    public class TagBuilderTest
    {
        [Test()]
        public void TestNoTags()
        {
            TagBuilder builder = new TagBuilder();
            Event ev = EventFactory.CreateFromXml(TestEventXml.E001_Error);
            builder.Process(ev);
            Assert.That(ev.Tags.Count, Is.EqualTo(0));
        }

        [Test()]
        public void TestCreateSocket()
        {
            TagBuilder builder = new TagBuilder();
            Event ev = EventFactory.CreateFromXml(TestEventXml.E083_CreateSocket);
            builder.Process(ev);
            Assert.That(ev.Tags.Count, Is.EqualTo(1));
            Assert.That(ev.Tags[0].Name, Is.EqualTo("Socket"));
            ResourceTag resTag = ev.Tags[0] as ResourceTag;
            Assert.That(resTag, Is.Not.Null);
            Assert.That(resTag.ResourceHandle, Is.EqualTo(0x8ac));
        }

        [Test()]
        public void TestConnectSocket()
        {
            TagBuilder builder = new TagBuilder();
            Event ev = EventFactory.CreateFromXml(TestEventXml.E084_Connect);
            builder.Process(ev);
            Assert.That(ev.Tags.Count, Is.EqualTo(1));
            Assert.That(ev.Tags[0].Name, Is.EqualTo("Socket"));
            ResourceTag resTag = ev.Tags[0] as ResourceTag;
            Assert.That(resTag, Is.Not.Null);
            Assert.That(resTag.ResourceHandle, Is.EqualTo(0x8ac));
        }

        [Test()]
        public void TestSameSocketTag()
        {
            TagBuilder builder = new TagBuilder();

            Event createEvent = EventFactory.CreateFromXml(TestEventXml.E083_CreateSocket);
            builder.Process(createEvent);
            Event connEvent = EventFactory.CreateFromXml(TestEventXml.E084_Connect);
            builder.Process(connEvent);

            Assert.That(createEvent.Tags[0], Is.SameAs(connEvent.Tags[0]));
        }
    }
}
