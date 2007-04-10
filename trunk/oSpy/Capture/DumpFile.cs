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
using ICSharpCode.SharpZipLib.BZip2;
using System.Xml;

namespace oSpy.Capture
{
    public class DumpFile
    {
        private string tmpPath;
        private StreamReader tmpReader;

        private SortedDictionary<uint, DumpEvent> events;
        public SortedDictionary<uint, DumpEvent> Events
        {
            get { return events; }
        }

        public DumpFile()
        {
        }

        public void Close()
        {
            if (tmpReader != null)
            {
                tmpReader.Close();
                tmpReader = null;
            }

            if (tmpPath != null)
            {
                File.Delete(tmpPath);
                tmpPath = null;
            }

            events = null;
        }

        public void Load(string path, IProgressFeedback progress)
        {
            BZip2InputStream inStream = new BZip2InputStream(new FileStream(path, FileMode.Open));
            XmlTextReader xtr = new XmlTextReader(inStream);

            tmpPath = Path.GetTempFileName();
            FileStream tmpFileStream = new FileStream(tmpPath, FileMode.Create, FileAccess.ReadWrite);
            tmpReader = new StreamReader(tmpFileStream);
            StreamWriter tmpFileWriter = new StreamWriter(tmpFileStream, Encoding.UTF8);

            events = new SortedDictionary<uint, DumpEvent>();

            int prevPct = -1, pct;
            while (xtr.Read())
            {
                pct = (int)(((float)inStream.Position / (float)inStream.Length) * 100.0f);
                if (pct != prevPct)
                {
                    prevPct = pct;
                    progress.ProgressUpdate("Loading", pct);
                }

                if (xtr.NodeType == XmlNodeType.Element && xtr.Name == "event")
                {
                    tmpFileWriter.Flush();
                    long startOffset = tmpFileStream.Position;

                    XmlReader rdr = xtr.ReadSubtree();
                    XmlDocument doc = new XmlDocument();
                    doc.Load(rdr);

                    XmlAttributeCollection attrs = doc.DocumentElement.Attributes;
                    uint id = Convert.ToUInt32(attrs["id"].Value);
                    DumpEventType type = (DumpEventType) Enum.Parse(typeof(DumpEventType), attrs["type"].Value);
                    DateTime timestamp = DateTime.FromFileTimeUtc(Convert.ToInt64(attrs["timestamp"].Value));
                    string processName = attrs["processName"].Value;
                    uint processId = Convert.ToUInt32(attrs["processId"].Value);
                    uint threadId = Convert.ToUInt32(attrs["threadId"].Value);

                    string eventStr = doc.DocumentElement.InnerXml;
                    tmpFileWriter.Write(eventStr);

                    events[id] = new DumpEvent(this, id, type, timestamp, processName, processId, threadId, startOffset, eventStr.Length);
                }
            }

            xtr.Close();

            tmpFileStream.Seek(0, SeekOrigin.Begin);
        }

        public string ExtractEventData(long offset, int length)
        {
            tmpReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            tmpReader.DiscardBufferedData();

            char[] buf = new char[length];
            tmpReader.Read(buf, 0, buf.Length);

            return new string(buf);
        }
    }

    public enum DumpEventType
    {
        Debug,
        Info,
        Warning,
        Error,
        FunctionCall
    }

    public class DumpEvent
    {
        private DumpFile file;
        public DumpFile File
        {
            get { return file; }
        }

        private uint id;
        public uint Id
        {
            get { return id; }
        }

        private DumpEventType type;
        public DumpEventType Type
        {
            get { return type; }
        }

        private DateTime timestamp;
        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        private string processName;
        public string ProcessName
        {
            get { return processName; }
        }

        private uint processId;
        public uint ProcessId
        {
            get { return processId; }
        }

        private uint threadId;
        public uint ThreadId
        {
            get { return threadId; }
        }

        private long dataOffset;
        public long DataOffset
        {
            get { return dataOffset; }
        }

        private int dataLength;
        public int DataLength
        {
            get { return dataLength; }
        }

        public string Data
        {
            get { return file.ExtractEventData(dataOffset, dataLength); }
        }

        public DumpEvent(DumpFile file,
                         uint id, DumpEventType type, DateTime timestamp, string processName, uint processId, uint threadId,
                         long dataOffset, int dataLength)
        {
            this.file = file;

            this.id = id;
            this.type = type;
            this.timestamp = timestamp;
            this.processName = processName;
            this.processId = processId;
            this.threadId = threadId;

            this.dataOffset = dataOffset;
            this.dataLength = dataLength;
        }
    }
}
