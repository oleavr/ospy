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
    class DataStream : FileStream
    {
        public DataStream ()
            : base ("c:\\foo", FileMode.Open)
        {
        }
    }

    internal class DataTransferStream : Stream
    {
        private DataTransfer transfer;
        private long position = 0;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return transfer.Size; }
        }

        public override long Position
        {
            get { return position; }
            set { position = value; }
        }

        public DataTransferStream (DataTransfer transfer)
        {
            this.transfer = transfer;
        }

        public override void Flush ()
        {
            throw new System.Exception ("The method or operation is not implemented.");
        }

        public override long Seek (long offset, SeekOrigin origin)
        {
            throw new System.Exception ("The method or operation is not implemented.");
        }

        public override void SetLength (long value)
        {
            throw new System.Exception ("The method or operation is not implemented.");
        }

        public override int Read (byte[] buffer, int offset, int count)
        {
            long resultLen = Math.Min (count, Length - position);
            if (resultLen <= 0)
                return 0;

            Array.Copy (transfer.Data, position, buffer, offset, resultLen);

            return (int) resultLen;
        }

        public override void Write (byte[] buffer, int offset, int count)
        {
            throw new System.Exception ("The method or operation is not implemented.");
        }
    }
}
