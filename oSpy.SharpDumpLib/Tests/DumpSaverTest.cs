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

using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture()]
    public class DumpSaverTest
    {
        [Test()]
        public void SaveUncompressed()
        {
            SaveAndVerifyOutput(DumpFormat.Uncompressed);
        }

        [Test()]
        public void SaveCompressed()
        {
            SaveAndVerifyOutput(DumpFormat.Compressed);
        }

        [Test()]
        public void SaveFullCycleUncompressed()
        {
            SaveAndLoadFullCycle(DumpFormat.Uncompressed);
        }

        [Test()]
        public void SaveFullCycleCompressed()
        {
            SaveAndLoadFullCycle(DumpFormat.Compressed);
        }

        private void SaveAndVerifyOutput(DumpFormat dumpFormat)
        {
            DumpSaver saver = new DumpSaver();
            MemoryStream outstream = new MemoryStream();
            Dump dump = GenerateTestDump();
            saver.Save(dump, dumpFormat, outstream);
            byte[] buffer = outstream.ToArray();

            Stream stream = new MemoryStream(buffer);
            BinaryReader reader = new BinaryReader(stream);

            ReadAndVerifyHeader(reader, dumpFormat);
            ReadAndVerifyBody(stream, dumpFormat);

            Assert.That(stream.Position, Is.EqualTo(stream.Length),
                         "Should not be any trailing data");
        }

        private void ReadAndVerifyHeader(BinaryReader reader, DumpFormat dumpFormat)
        {
            string magic = new string(reader.ReadChars(4));
            uint version = reader.ReadUInt32();
            uint isCompressed = reader.ReadUInt32();
            uint numEvents = reader.ReadUInt32();
            Assert.That(magic, Is.EqualTo("oSpy"));
            Assert.That(version, Is.EqualTo(2));
            if (dumpFormat == DumpFormat.Compressed)
                Assert.That(isCompressed, Is.EqualTo(1));
            else
                Assert.That(isCompressed, Is.EqualTo(0));
            Assert.That(numEvents, Is.EqualTo(3));
        }

        private void ReadAndVerifyBody(Stream stream, DumpFormat dumpFormat)
        {
            XmlDocument doc = new XmlDocument();
            if (dumpFormat == DumpFormat.Compressed)
                stream = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(stream);
            doc.Load(stream);
            Assert.That(doc.DocumentElement.ChildNodes.Count, Is.EqualTo(3));
            Assert.That(doc.DocumentElement.ChildNodes.Item(0).OuterXml,
                        Is.EqualTo(XmlString.Canonicalize(TestEventXml.E001_Error)));
            Assert.That(doc.DocumentElement.ChildNodes.Item(1).OuterXml,
                        Is.EqualTo(XmlString.Canonicalize(TestEventXml.E083_CreateSocket)));
            Assert.That(doc.DocumentElement.ChildNodes.Item(2).OuterXml,
                        Is.EqualTo(XmlString.Canonicalize(TestEventXml.E084_Connect)));
        }

        private void SaveAndLoadFullCycle(DumpFormat dumpFormat)
        {
            DumpSaver saver = new DumpSaver();
            MemoryStream outStream = new MemoryStream();
            Dump canonicalDump = GenerateTestDump();
            saver.Save(canonicalDump, dumpFormat, outStream);

            DumpLoader loader = new DumpLoader();
            Dump loadedDump = loader.Load(new MemoryStream(outStream.ToArray()));
            foreach (KeyValuePair<uint, Event> entry in loadedDump.Events)
            {
                Assert.That(entry.Value.RawData, Is.EqualTo(canonicalDump.Events[entry.Key].RawData));
            }
        }

        private Dump GenerateTestDump()
        {
            Dump dump = new Dump();
            dump.AddEvent(EventFactory.CreateFromXml(TestEventXml.E001_Error));
            dump.AddEvent(EventFactory.CreateFromXml(TestEventXml.E083_CreateSocket));
            dump.AddEvent(EventFactory.CreateFromXml(TestEventXml.E084_Connect));
            return dump;
        }
    }
}
