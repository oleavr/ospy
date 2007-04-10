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
using System.Data;

namespace oSpy.Capture
{
    public class DumpFile
    {
        private string name = "";
        public string Name
        {
            get { return name; }
        }

        private DataSet events;
        public DataSet Events
        {
            get { return events; }
        }

        private string uncompPath, cachePath;
        private StreamReader cacheReader;

        private SortedDictionary<uint, KeyValuePair<long, int>> evDataOffsets;

        public DumpFile()
        {
        }

        public DumpFile(string uncompPath)
        {
            this.uncompPath = uncompPath;
        }

        public void Close()
        {
            if (cacheReader != null)
            {
                cacheReader.Close();
                cacheReader = null;
            }

            if (cachePath != null)
            {
                File.Delete(cachePath);
                cachePath = null;
            }

            if (uncompPath != null)
            {
                File.Delete(uncompPath);
                uncompPath = null;
            }

            evDataOffsets = null;
        }

        public void Load(IProgressFeedback progress)
        {
            Load(null, progress);
        }

        public DataSet Load(string path, IProgressFeedback progress)
        {
            int prevPct, pct;

            if (uncompPath == null)
            {
                uncompPath = Path.GetTempFileName();

                BZip2InputStream inStream = new BZip2InputStream(new FileStream(path, FileMode.Open));
                FileStream outStream = new FileStream(uncompPath, FileMode.Append);
                byte[] buf = new byte[64 * 1024];

                prevPct = -1;
                while (true)
                {
                    pct = (int)(((float)inStream.Position / (float)inStream.Length) * 100.0f);
                    if (pct != prevPct)
                    {
                        prevPct = pct;
                        progress.ProgressUpdate("Uncompressing", pct);
                    }

                    int n = inStream.Read(buf, 0, buf.Length);
                    if (n == 0)
                        break;

                    outStream.Write(buf, 0, n);
                }
                outStream.Close();
                inStream.Close();
            }

            FileStream uncompStream = new FileStream(uncompPath, FileMode.Open);
            XmlTextReader xtr = new XmlTextReader(uncompStream);

            cachePath = Path.GetTempFileName();

            FileStream cacheFileStream = new FileStream(cachePath, FileMode.Create, FileAccess.ReadWrite);
            cacheReader = new StreamReader(cacheFileStream);
            StreamWriter cacheFileWriter = new StreamWriter(cacheFileStream, Encoding.UTF8);

            DataSet evDataSet = CreateDataSet();
            DataTable tbl = evDataSet.Tables["events"];
            tbl.BeginLoadData();

            evDataOffsets = new SortedDictionary<uint, KeyValuePair<long, int>>();

            prevPct = -1;
            while (xtr.Read())
            {
                pct = (int)(((float)uncompStream.Position / (float)uncompStream.Length) * 100.0f);
                if (pct != prevPct)
                {
                    prevPct = pct;
                    progress.ProgressUpdate("Indexing", pct);
                }

                if (xtr.NodeType == XmlNodeType.Element && xtr.Name == "event")
                {
                    cacheFileWriter.Flush();
                    long startOffset = cacheFileStream.Position;

                    XmlReader rdr = xtr.ReadSubtree();
                    XmlDocument doc = new XmlDocument();
                    doc.Load(rdr);

                    XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

                    DataRow row = tbl.NewRow();
                    uint id = Convert.ToUInt32(attrs["id"].Value);
                    row[0] = id;
                    row[1] = (DumpEventType) Enum.Parse(typeof(DumpEventType), attrs["type"].Value);
                    row[2] = DateTime.FromFileTimeUtc(Convert.ToInt64(attrs["timestamp"].Value));
                    row[3] = attrs["processName"].Value;
                    row[4] = Convert.ToUInt32(attrs["processId"].Value);
                    row[5] = Convert.ToUInt32(attrs["threadId"].Value);
                    row.AcceptChanges();
                    tbl.Rows.Add(row);

                    string eventStr = doc.DocumentElement.OuterXml;
                    cacheFileWriter.Write(eventStr);
                    cacheFileWriter.Flush();

                    evDataOffsets[id] = new KeyValuePair<long, int>(startOffset, eventStr.Length);
                }
            }

            tbl.EndLoadData();

            xtr.Close();

            cacheFileStream.Seek(0, SeekOrigin.Begin);

            events = evDataSet;
            return evDataSet;
        }

        public void Save(string path, IProgressFeedback progress)
        {
            int prevPct, pct;

            FileStream inStream = new FileStream(uncompPath, FileMode.Open);
            BZip2OutputStream outStream = new BZip2OutputStream(new FileStream(path, FileMode.Create));
            byte[] buf = new byte[64 * 1024];

            prevPct = -1;
            while (true)
            {
                pct = (int)(((float)inStream.Position / (float)inStream.Length) * 100.0f);
                if (pct != prevPct)
                {
                    prevPct = pct;
                    progress.ProgressUpdate("Saving", pct);
                }

                int n = inStream.Read(buf, 0, buf.Length);
                if (n == 0)
                    break;

                outStream.Write(buf, 0, n);
            }
            outStream.Close();
            inStream.Close();
        }

        private DataSet CreateDataSet()
        {
            DataSet ds = new DataSet("dumpFile");

            DataTable tbl = new DataTable("events");
            DataColumnCollection cols = tbl.Columns;
            cols.Add("id", typeof(uint));
            cols.Add("type", typeof(DumpEventType));
            cols.Add("timestamp", typeof(DateTime));
            cols.Add("processName", typeof(string));
            cols.Add("processId", typeof(uint));
            cols.Add("threadId", typeof(uint));
            ds.Tables.Add(tbl);

            return ds;
        }

        public string ExtractEventData(uint id)
        {
            KeyValuePair<long, int> pair = evDataOffsets[id];
            long offset = pair.Key;
            int length = pair.Value;

            cacheReader.BaseStream.Seek(offset, SeekOrigin.Begin);
            cacheReader.DiscardBufferedData();

            char[] buf = new char[length];
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
}
