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
