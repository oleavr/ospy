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
        public void NoTags()
        {
            TagBuilder builder = new TagBuilder();
            Event ev = EventFactory.CreateFromXml(TestEventXml.E001_Error);
            builder.Process(ev);
            Assert.That(ev.Tags.Count, Is.EqualTo(0));
        }

        [Test()]
        public void CreateSocket()
        {
            BuildAndVerifySocketTag(TestEventXml.E083_CreateSocket, 0x8ac);
        }

        [Test()]
        public void CloseSocket()
        {
            BuildAndVerifySocketTag(TestEventXml.E140_CloseSocket, 0x8ac);
        }

        [Test()]
        public void ConnectSocket()
        {
            BuildAndVerifySocketTag(TestEventXml.E084_Connect, 0x8ac);
        }

        [Test()]
        public void SendSocket()
        {
            BuildAndVerifySocketTag(TestEventXml.E096_Send, 0x8ac);
        }

        [Test()]
        public void ReceiveSocket()
        {
            BuildAndVerifySocketTag(TestEventXml.E130_Receive, 0x8ac);
        }
        
        private void BuildAndVerifySocketTag(string xml, uint expectedHandle)
        {
            TagBuilder builder = new TagBuilder();
            Event ev = EventFactory.CreateFromXml(xml);
            builder.Process(ev);
            Assert.That(ev.Tags.Count, Is.EqualTo(1));
            Assert.That(ev.Tags[0], Is.InstanceOfType(typeof(Socket.SocketResourceTag)));
            Assert.That(ev.Tags[0].Name, Is.EqualTo("Socket"));
            ResourceTag resTag = ev.Tags[0] as ResourceTag;
            Assert.That(resTag.ResourceHandle, Is.EqualTo(expectedHandle));
        }

        [Test()]
        public void SameSocketTag()
        {
            TagBuilder builder = new TagBuilder();

            Event firstCreateEvent = EventFactory.CreateFromXml(TestEventXml.E083_CreateSocket);
            builder.Process(firstCreateEvent);
            Event firstConnEvent = EventFactory.CreateFromXml(TestEventXml.E084_Connect);
            builder.Process(firstConnEvent);

            Event secondCreateEvent = EventFactory.CreateFromXml(TestEventXml.E141_CreateSocket);
            builder.Process(secondCreateEvent);
            Event secondConnEvent = EventFactory.CreateFromXml(TestEventXml.E142_Connect);
            builder.Process(secondConnEvent);

            Assert.That(firstCreateEvent.Tags[0], Is.SameAs(firstConnEvent.Tags[0]));
            Assert.That(secondCreateEvent.Tags[0], Is.SameAs(secondConnEvent.Tags[0]));
            Assert.That(firstCreateEvent.Tags[0], Is.Not.SameAs(secondCreateEvent.Tags[0]));
        }
    }
}
