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
