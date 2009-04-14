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
            DumpLoader loader = new DumpLoader ();
            Stream stream = TestOsdStream.GenerateFromXmlEvents (TestEventXml.E001_Error, TestEventXml.E083_CreateSocket, TestEventXml.E140_CloseSocket);
            Dump dump = loader.Load (stream);
            Assert.That (dump.Events.Count, Is.EqualTo (3));
            Assert.That (dump.Events.Keys, Is.EquivalentTo (new uint[] { 1, 83, 140 }));
            Assert.That (dump.Events[1].Id, Is.EqualTo (1));
            Assert.That (dump.Events[83].Id, Is.EqualTo (83));
            Assert.That (dump.Events[140].Id, Is.EqualTo (140));
        }
    }
}
