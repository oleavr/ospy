//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using oSpy.Util;
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpy.Capture
{
    class Converter
    {
        public void ConvertAll(string captureDirPath, int numEvents, IProgressFeedback progress)
        {
            List<BinaryReader> readers = new List<BinaryReader>(1);
            SortedList<uint, KeyValuePair<BinaryReader, uint>> ids = new SortedList<uint, KeyValuePair<BinaryReader, uint>>(numEvents);

            uint i = 0;
            foreach (string filePath in Directory.GetFiles(captureDirPath, "*.log", SearchOption.TopDirectoryOnly))
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryReader r = new BinaryReader(fs);

                readers.Add(r);

                while (fs.Position < fs.Length)
                {
                    i++;
                    int pct = (int)(((float)i / (float)numEvents) * 100.0f);
                    progress.ProgressUpdate("Indexing", pct);

                    uint id = r.ReadUInt32();
                    uint size = r.ReadUInt32();

                    ids.Add(id, new KeyValuePair<BinaryReader, uint>(r, (uint)fs.Position));

                    fs.Seek(size, SeekOrigin.Current);
                }
            }

            string resultPath = String.Format("{0}\\capture.osd", captureDirPath);
            BZip2OutputStream outStream = new BZip2OutputStream(new FileStream(resultPath, FileMode.Create));

            XmlTextWriter xtw = new XmlTextWriter(outStream, System.Text.Encoding.UTF8);
            xtw.Formatting = Formatting.Indented;
            xtw.Indentation = 4;
            xtw.IndentChar = ' ';
            xtw.WriteStartDocument(true);
            xtw.WriteStartElement("events");

            i = 0;
            foreach (KeyValuePair<BinaryReader, uint> pair in ids.Values)
            {
                i++;
                int pct = (int)(((float)i / (float)numEvents) * 100.0f);
                progress.ProgressUpdate(String.Format("Converting event {0} of {1}", i, numEvents), pct);

                BinaryReader r = pair.Key;
                uint offset = pair.Value;

                r.BaseStream.Seek(offset, SeekOrigin.Begin);
                UnserializeNode(r, xtw);
            }

            xtw.WriteEndElement();
            xtw.WriteEndDocument();
            xtw.Close();

            foreach (BinaryReader r in readers)
                r.Close();
        }

        private void UnserializeNode(BinaryReader r, XmlTextWriter xtw)
        {
            xtw.WriteStartElement(UnserializeString(r));

            uint attrCount = r.ReadUInt32();
            for (int i = 0; i < attrCount; i++)
            {
                xtw.WriteAttributeString(UnserializeString(r), UnserializeString(r));
            }

            UInt32 contentIsRaw = r.ReadUInt32();
            string content = "";

            if (contentIsRaw != 0)
            {
                uint len = r.ReadUInt32();
                byte[] bytes = r.ReadBytes((int) len);
                if (bytes.Length > 0)
                {
                    content = Convert.ToBase64String(bytes, Base64FormattingOptions.None);
                }
            }
            else
            {
                content = UnserializeString(r);
            }

            if (content != String.Empty)
            {
                xtw.WriteString(content);
            }

            uint childCount = r.ReadUInt32();
            for (int i = 0; i < childCount; i++)
            {
                UnserializeNode(r, xtw);
            }

            xtw.WriteEndElement();
        }

        private string UnserializeString(BinaryReader r)
        {
            uint len = r.ReadUInt32();
            byte[] buf = r.ReadBytes((int) len);
            return StaticUtils.DecodeASCII(buf);
        }
    }
}
