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

using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpy.SharpDumpLib.Tests
{
    public class TestOsdStream
    {
        public static Stream GenerateUncompressedFrom(params string[] xmlEvents)
        {
            return GenerateFrom(false, xmlEvents);
        }

        public static Stream GenerateCompressedFrom(params string[] xmlEvents)
        {
            return GenerateFrom(true, xmlEvents);
        }

        private static Stream GenerateFrom(bool compressed, params string[] xmlEvents)
        {
            byte[] header;
            byte[] body;

            using (MemoryStream header_stream = new MemoryStream())
            {
                using (BinaryWriter headerWriter = new BinaryWriter(header_stream, Encoding.ASCII))
                {
                    byte[] magic = Encoding.ASCII.GetBytes("oSpy");
                    const uint version = 2;
                    uint isCompressed = (compressed) ? 1U : 0U;
                    uint numEvents = (uint)xmlEvents.Length;

                    headerWriter.Write(magic);
                    headerWriter.Write(version);
                    headerWriter.Write(isCompressed);
                    headerWriter.Write(numEvents);
                }

                header = header_stream.ToArray();
            }

            using (MemoryStream rawBodyStream = new MemoryStream())
            {
                Stream bodyStream;

                if (compressed)
                    bodyStream = new BZip2OutputStream(rawBodyStream);
                else
                    bodyStream = rawBodyStream;

                using (BinaryWriter bodyWriter = new BinaryWriter(bodyStream, Encoding.UTF8))
                {
                    bodyWriter.Write(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><events>"));
                    foreach (string xml in xmlEvents)
                        bodyWriter.Write(Encoding.UTF8.GetBytes(xml));
                    bodyWriter.Write(Encoding.UTF8.GetBytes("</events>"));
                }

                bodyStream.Flush();
                body = rawBodyStream.ToArray();
            }

            byte[] result = new byte[header.Length + body.Length];
            header.CopyTo(result, 0);
            body.CopyTo(result, header.Length);
            return new MemoryStream(result);
        }
    }
}
