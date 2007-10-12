//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
