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
