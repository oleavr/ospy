using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Net
{
    [Serializable()]
    public class PacketSlice : IComparable
    {
        public IPPacket Packet;
        public int Offset;
        public int Length;
        public int Tag;

        public PacketSlice(PacketSlice srcSlice)
        {
            Packet = srcSlice.Packet;
            Offset = srcSlice.Offset;
            Length = srcSlice.Length;
            Tag = srcSlice.Tag;
        }

        public PacketSlice(IPPacket packet, int offset, int length)
        {
            Initialize(packet, offset, length, -1);
        }

        public PacketSlice(IPPacket packet, int offset, int length, int tag)
        {
            Initialize(packet, offset, length, tag);
        }

        private void Initialize(IPPacket packet, int offset, int length, int tag)
        {
            Packet = packet;
            Offset = offset;
            Length = length;
            Tag = tag;
        }

        public int CompareTo(Object obj)
        {
            PacketSlice otherSlice = obj as PacketSlice;

            int result = Packet.Index.CompareTo(otherSlice.Packet.Index);
            if (result != 0)
                return result;

            return Offset.CompareTo(otherSlice.Offset);
        }
    }
}
