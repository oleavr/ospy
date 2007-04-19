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

namespace oSpy.SharpDumpLib
{
    public class BulkStorage
    {
        private string tmpFilePath = null;
        private Stream stream = null;

        public BulkStorage()
        {
            tmpFilePath = Path.GetTempFileName();

            stream = File.Open(tmpFilePath, FileMode.Create, FileAccess.ReadWrite);
        }

        public void Close()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (tmpFilePath != null)
            {
                File.Delete(tmpFilePath);
                tmpFilePath = null;
            }
        }

        public BulkSlot AppendData(byte[] data)
        {
            long offset = stream.Position;
            stream.Write(data, 0, data.Length);
            return new BulkSlot(this, offset, data.Length);
        }

        public byte[] GetData(BulkSlot slot)
        {
            stream.Seek(slot.Offset, SeekOrigin.Begin);
            byte[] buf = new byte[slot.Size];
            stream.Read(buf, 0, buf.Length);
            return buf;
        }
    }

    public class BulkSlot
    {
        private BulkStorage storage;
        public BulkStorage Storage
        {
            get { return storage; }
        }

        private long offset;
        public long Offset
        {
            get { return offset; }
        }

        private int size;
        public int Size
        {
            get { return size; }
        }

        public byte[] Data
        {
            get { return storage.GetData(this); }
        }

        public BulkSlot(BulkStorage storage, long offset, int size)
        {
            this.storage = storage;
            this.offset = offset;
            this.size = size;
        }
    }
}
