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
