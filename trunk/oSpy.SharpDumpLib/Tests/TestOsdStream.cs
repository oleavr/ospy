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
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpy.SharpDumpLib.Tests
{
    public class TestOsdStream
    {
        public static Stream GenerateUncompressedFrom (params string[] xmlEvents)
        {
            return GenerateFrom (false, xmlEvents);
        }

        public static Stream GenerateCompressedFrom (params string[] xmlEvents)
        {
            return GenerateFrom (true, xmlEvents);
        }

        private static Stream GenerateFrom (bool compressed, params string[] xmlEvents)
        {
            byte[] header;
            byte[] body;

            using (MemoryStream header_stream = new MemoryStream ()) {
                using (BinaryWriter header_writer = new BinaryWriter (header_stream, Encoding.ASCII)) {
                    byte[] magic = Encoding.ASCII.GetBytes ("oSpy");
                    const uint version = 2;
                    uint is_compressed = (compressed) ? 1U : 0U;
                    uint num_events = (uint) xmlEvents.Length;

                    header_writer.Write (magic);
                    header_writer.Write (version);
                    header_writer.Write (is_compressed);
                    header_writer.Write (num_events);
                }

                header = header_stream.ToArray ();
            }

            using (MemoryStream raw_body_stream = new MemoryStream ()) {
                Stream body_stream;

                if (compressed)
                    body_stream = new BZip2OutputStream (raw_body_stream);
                else
                    body_stream = raw_body_stream;

                using (BinaryWriter body_writer = new BinaryWriter (body_stream, Encoding.UTF8)) {
                    body_writer.Write (Encoding.UTF8.GetBytes ("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><events>"));
                    foreach (string xml in xmlEvents)
                        body_writer.Write (Encoding.UTF8.GetBytes (xml));
                    body_writer.Write (Encoding.UTF8.GetBytes ("</events>"));
                }

                body_stream.Flush ();
                body = raw_body_stream.ToArray ();
            }

            byte[] result = new byte[header.Length + body.Length];
            header.CopyTo (result, 0);
            body.CopyTo (result, header.Length);
            return new MemoryStream (result);
        }
    }
}
