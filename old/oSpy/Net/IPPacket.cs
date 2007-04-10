//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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

namespace oSpy.Net
{
    [Serializable()]
    public class IPPacket : IComparable
    {
        protected int index;
        public int Index
        {
            get { return index; }
        }

        protected DateTime timestamp;
        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        protected UInt32 resourceId;
        public UInt32 ResourceId
        {
            get { return resourceId; }
        }

        protected PacketDirection direction;
        public PacketDirection Direction
        {
            get { return direction; }
        }

        protected IPEndpoint localEndpoint;
        public IPEndpoint LocalEndpoint
        {
            get { return localEndpoint; }
        }

        protected IPEndpoint remoteEndpoint;
        public IPEndpoint RemoteEndpoint
        {
            get { return remoteEndpoint; }
        }

        protected byte[] bytes;
        public byte[] Bytes
        {
            get { return bytes; }
        }

        protected object tag;
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        public IPPacket(int index,
                        UInt32 resourceId,
                        PacketDirection direction,
                        IPEndpoint localEndpoint,
                        IPEndpoint remoteEndpoint,
                        byte[] bytes)
        {
            Initialize(index, DateTime.Now, resourceId, direction, localEndpoint, remoteEndpoint, bytes);
        }

        public IPPacket(int index,
                        DateTime timestamp,
                        UInt32 resourceId,
                        PacketDirection direction,
                        IPEndpoint localEndpoint,
                        IPEndpoint remoteEndpoint,
                        byte[] bytes)
        {
            Initialize(index, timestamp, resourceId, direction, localEndpoint, remoteEndpoint, bytes);
        }

        private void Initialize(int index,
                                DateTime timestamp,
                                UInt32 resourceId,
                                PacketDirection direction,
                                IPEndpoint localEndpoint,
                                IPEndpoint remoteEndpoint,
                                byte[] bytes)
        {
            this.index = index;
            this.timestamp = timestamp;
            this.resourceId = resourceId;
            this.direction = direction;
            this.localEndpoint = localEndpoint;
            this.remoteEndpoint = remoteEndpoint;
            this.bytes = bytes;
        }

        public int CompareTo(Object obj)
        {
            IPPacket otherPacket = obj as IPPacket;

            return index.CompareTo(otherPacket.Index);
        }
    }

}
