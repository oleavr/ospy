/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
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
