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

namespace oSpy.SharpDumpLib
{
    public class Dump
    {
        private string cachePath = null;
        private StreamReader cacheReader = null;
        private StreamWriter cacheWriter = null;

        private SortedDictionary<uint, Event> events = new SortedDictionary<uint, Event>();
        public SortedDictionary<uint, Event> Events
        {
            get { return events; }
        }

        public Dump()
        {
            cachePath = Path.GetTempFileName();

            FileStream fs = new FileStream(cachePath, FileMode.Create, FileAccess.ReadWrite);
            cacheReader = new StreamReader(fs);
            cacheWriter = new StreamWriter(fs, Encoding.UTF8);
        }

        public void Close()
        {
            if (cachePath != null)
            {
                cacheReader.Close();
                cacheReader = null;
                cacheWriter = null;

                File.Delete(cachePath);
                cachePath = null;
            }

            events.Clear();
        }

        public void AddEvent(XmlElement el)
        {
            XmlAttributeCollection attrs = el.Attributes;

            uint id = Convert.ToUInt32(attrs["id"].Value);
            DumpEventType type = (DumpEventType)Enum.Parse(typeof(DumpEventType), attrs["type"].Value);
            DateTime timestamp = DateTime.FromFileTimeUtc(Convert.ToInt64(attrs["timestamp"].Value));
            string processName = attrs["processName"].Value;
            uint processId = Convert.ToUInt32(attrs["processId"].Value);
            uint threadId = Convert.ToUInt32(attrs["threadId"].Value);

            cacheWriter.Flush();
            long startOffset = cacheWriter.BaseStream.Position;

            string eventStr = el.OuterXml;
            cacheWriter.Write(eventStr);
            cacheWriter.Flush();

            events[id] = new Event(this, id, type, timestamp, processName, processId, threadId,
                                   startOffset, eventStr.Length);
        }

        public string ExtractEventData(long offset, int numChars)
        {
            cacheReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            cacheReader.DiscardBufferedData();

            char[] buf = new char[numChars];
            cacheReader.Read(buf, 0, buf.Length);

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

    public class Event
    {
        private Dump dump;
        public Dump Dump
        {
            get { return dump; }
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
        private int dataNumChars;

        public XmlElement Data
        {
            get { return GetDataAsElement(); }
        }

        public string RawData
        {
            get { return GetData(); }
        }

        public Event(Dump dump, uint id, DumpEventType type, DateTime timestamp, string processName, uint processId, uint threadId,
                     long dataOffset, int dataNumChars)
        {
            this.dump = dump;
            this.id = id;
            this.type = type;
            this.timestamp = timestamp;
            this.processName = processName;
            this.processId = processId;
            this.threadId = threadId;

            this.dataOffset = dataOffset;
            this.dataNumChars = dataNumChars;
        }

        private string GetData()
        {
            return dump.ExtractEventData(dataOffset, dataNumChars);
        }

        private XmlElement GetDataAsElement()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(new StringReader(GetData()));
            return doc.DocumentElement;
        }
    }
}
