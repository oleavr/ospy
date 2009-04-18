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
using System.ComponentModel;
using System.IO;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpy.SharpDumpLib.Tests
{
    [TestFixture()]
    public class DumpLoaderTest
    {
        [Test()]
        public void LoadUncompressed()
        {
            Stream stream = TestOsdStream.GenerateUncompressedFrom(TestEventXml.E001_Error, TestEventXml.E083_CreateSocket, TestEventXml.E140_CloseSocket);
            LoadAndVerifyEvents(stream);
        }

        [Test()]
        public void LoadCompressed()
        {
            Stream stream = TestOsdStream.GenerateCompressedFrom(TestEventXml.E001_Error, TestEventXml.E083_CreateSocket, TestEventXml.E140_CloseSocket);
            LoadAndVerifyEvents(stream);
        }

        [Test ()]
        public void LoadAsync()
        {
            Stream stream = TestOsdStream.GenerateUncompressedFrom(TestEventXml.E001_Error, TestEventXml.E083_CreateSocket, TestEventXml.E140_CloseSocket);
            DumpLoader loader = new DumpLoader();

            AutoResetEvent completed = new AutoResetEvent(false);
            Dump dump = null;
            List<ProgressChangedEventArgs> progressEvents = new List<ProgressChangedEventArgs>();
            loader.LoadProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
                                          {
                                              lock (progressEvents)
                                                  progressEvents.Add(e);
                                          };
            loader.LoadCompleted += delegate(object sender, LoadCompletedEventArgs e)
                                    {
                                        dump = e.Dump;
                                        completed.Set();
                                    };
            loader.LoadAsync(stream, this);

            completed.WaitOne();

            VerifyEvents(dump);
            Assert.That(progressEvents.Count, Is.EqualTo(3));
            Assert.That(progressEvents[0].ProgressPercentage, Is.EqualTo(33));
            Assert.That(progressEvents[1].ProgressPercentage, Is.EqualTo(66));
            Assert.That(progressEvents[2].ProgressPercentage, Is.EqualTo(100));
        }

        private void LoadAndVerifyEvents(Stream stream)
        {
            DumpLoader loader = new DumpLoader();
            Dump dump = loader.Load(stream);
            VerifyEvents(dump);
        }

        private void VerifyEvents(Dump dump)
        {
            Assert.That(dump.Events.Count, Is.EqualTo(3));
            Assert.That(dump.Events.Keys, Is.EquivalentTo(new uint[] { 1, 83, 140 }));
            Assert.That(dump.Events[1].Id, Is.EqualTo(1));
            Assert.That(dump.Events[83].Id, Is.EqualTo(83));
            Assert.That(dump.Events[140].Id, Is.EqualTo(140));
        }
    }
}
