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

namespace oSpy.SharpDumpLib.Tests
{
    public class TestOsdStream : MemoryStream
    {
        private TestOsdStream (byte[] buffer)
            : base (buffer)
        {
        }
        
        public static TestOsdStream GenerateFromXmlEvents (params string[] xmlEvents)
        {
            byte[] buffer;
            
            using (MemoryStream stream = new MemoryStream ()) {
                using (BinaryWriter bw = new BinaryWriter (stream, Encoding.ASCII)) {
                    byte[] magic = Encoding.ASCII.GetBytes ("oSpy");
                    const uint version = 2;
                    const uint is_compressed = 0;
                    uint num_events = (uint) xmlEvents.Length;

                    bw.Write (magic);
                    bw.Write (version);
                    bw.Write (is_compressed);
                    bw.Write (num_events);

                    bw.Write (Encoding.UTF8.GetBytes ("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><events>"));
                    foreach (string xml in xmlEvents)
                        bw.Write (Encoding.UTF8.GetBytes (xml));
                    bw.Write (Encoding.UTF8.GetBytes ("</events>"));
                }

                buffer = stream.ToArray ();
            }

            return new TestOsdStream (buffer);
        }
    }
}
