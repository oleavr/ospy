//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
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
        FunctionCall,
        IOCTL_INTERNAL_USB_SUBMIT_URB,
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
