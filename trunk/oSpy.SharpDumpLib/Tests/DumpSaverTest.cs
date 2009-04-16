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
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture ()]
    public class DumpSaverTest
    {
        [Test ()]
        public void SaveUncompressed ()
        {
            DumpSaver saver = new DumpSaver ();
            Dump dump = new Dump ();
            dump.AddEvent (EventFactory.CreateFromXml (TestEventXml.E001_Error));
            dump.AddEvent (EventFactory.CreateFromXml (TestEventXml.E083_CreateSocket));
            dump.AddEvent (EventFactory.CreateFromXml (TestEventXml.E084_Connect));
            MemoryStream stream = new MemoryStream ();
            saver.Save (dump, stream);

            stream.Seek (0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader (stream);

            // Header
            string magic = new string (reader.ReadChars (4));
            uint version = reader.ReadUInt32 ();
            uint is_compressed = reader.ReadUInt32 ();
            uint num_events = reader.ReadUInt32 ();
            Assert.That (magic, Is.EqualTo ("oSpy"));
            Assert.That (version, Is.EqualTo (2));
            Assert.That (is_compressed, Is.EqualTo (0));
            Assert.That (num_events, Is.EqualTo (3));

            // Body
            XmlDocument doc = new XmlDocument ();
            doc.Load (stream);
            Assert.That (doc.DocumentElement.ChildNodes.Count, Is.EqualTo (3));
            Assert.That (doc.DocumentElement.ChildNodes.Item (0).OuterXml,
                         Is.EqualTo (XmlString.Canonicalize (TestEventXml.E001_Error)));
            Assert.That (doc.DocumentElement.ChildNodes.Item (1).OuterXml,
                         Is.EqualTo (XmlString.Canonicalize (TestEventXml.E083_CreateSocket)));
            Assert.That (doc.DocumentElement.ChildNodes.Item (2).OuterXml,
                         Is.EqualTo (XmlString.Canonicalize (TestEventXml.E084_Connect)));

            Assert.That (stream.Position, Is.EqualTo (stream.Length),
                         "Should not be any trailing data");
        }
    }
}
