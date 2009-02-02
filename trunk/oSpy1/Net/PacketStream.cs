/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *                     Frode Hus <husfro@gmail.com>
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
using System.IO;
using oSpy.Util;
namespace oSpy.Net
{
    [Serializable()]
    public class PacketStream : Stream
    {
        protected List<IPPacket> packets;
        public List<IPPacket> Packets
        {
            get { return packets; }
        }

        public IPPacket CurPacket
        {
            get
            {
                PacketSlice curSlice = GetCurPacketSlice();
                if (curSlice == null)
                    return null;

                return curSlice.Packet;
            }
        }

        protected List<PacketSlice> lastReadSlices;
        public List<PacketSlice> LastReadSlices
        {
            get { return lastReadSlices; }
        }

        private List<KeyValuePair<long, PacketSlice>> prevStates;

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        protected long position;
        public override long Position
        {
            get { return position; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        protected long length;
        public override long Length
        {
            get { return length; }
        }

        protected PacketSlice curSlice;

        public PacketStream()
        {
            packets = new List<IPPacket>();
            lastReadSlices = new List<PacketSlice>();
            prevStates = new List<KeyValuePair<long, PacketSlice>>();
        }

        public void ResetState()
        {
            position = 0;
            curSlice = null;
        }

        public void PushState()
        {
            prevStates.Add(
                new KeyValuePair<long, PacketSlice>(
                    position, (curSlice != null) ? new PacketSlice(curSlice) : null));
        }

        public void PopState()
        {
            KeyValuePair<long, PacketSlice> state = prevStates[prevStates.Count - 1];
            prevStates.RemoveAt(prevStates.Count - 1);
            position = state.Key;
            curSlice = state.Value;
        }

        public void AppendPacket(IPPacket packet)
        {
            packets.Add(packet);
            length += packet.Bytes.Length;
        }

        public override void Flush()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long end = length - 1;
            if (end < 0)
                end = 0;

            long dest;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    dest = offset;
                    break;
                case SeekOrigin.Current:
                    dest = position + offset;
                    break;
                case SeekOrigin.End:
                    dest = end + offset;
                    break;
                default:
                    dest = 0;
                    break;
            }

            position = dest;
            curSlice = null;

            return dest;
        }

        private PacketSlice GetCurPacketSlice()
        {
            if (curSlice == null)
                UpdateCurPacketSlice();

            return curSlice;
        }

        private void UpdateCurPacketSlice()
        {
            if (position < 0 || position >= length)
                return;

            int i = 0, offset = 0;
            foreach (IPPacket packet in packets)
            {
                int pktLen = packet.Bytes.Length;

                if (position < offset + pktLen)
                {
                    int pktOffset = (int)position - offset;

                    curSlice = new PacketSlice(packet, pktOffset, (int)pktLen - pktOffset, i);
                    return;
                }

                i++;
                offset += pktLen;
            }
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (curSlice == null)
            {
                UpdateCurPacketSlice();
                if (curSlice == null)
                    return 0;
            }

            lastReadSlices.Clear();

            long maxCount = length - position;
            if (maxCount <= 0)
                return 0;

            long totalBytesToRead = (count > maxCount) ? maxCount : count;
            long totalBytesLeft = totalBytesToRead;

            long dstOffset = offset;

            while (totalBytesLeft > 0)
            {
                long n = totalBytesLeft;
                if (n > curSlice.Length)
                    n = curSlice.Length;

                lastReadSlices.Add(new PacketSlice(curSlice.Packet, curSlice.Offset, (int)n));

                Array.Copy(curSlice.Packet.Bytes, curSlice.Offset, buffer, dstOffset, n);

                curSlice.Offset += (int)n;
                curSlice.Length -= (int)n;

                if (curSlice.Length == 0)
                {
                    int index = curSlice.Tag + 1;
                    if (index < packets.Count)
                    {
                        curSlice.Packet = packets[index];
                        curSlice.Offset = 0;
                        curSlice.Length = packets[index].Bytes.Length;
                        curSlice.Tag = index;
                    }
                    else
                    {
                        curSlice = null;
                    }
                }

                position += n;

                dstOffset += n;
                totalBytesLeft -= n;
            }

            return (int)totalBytesToRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public long GetBytesAvailable()
        {
            return length - position;
        }

        public UInt16 ReadU16LE()
        {
            return ReadU16LE(null);
        }

        public UInt16 ReadU16LE(List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(2);

            if (slicesRead != null)
            {
                slicesRead.Clear();
                slicesRead.AddRange(lastReadSlices);
            }

            return (UInt16)((UInt16)buf[1] << 8 |
                            (UInt16)buf[0]);
        }
        public Int16 PeekInt16()
        {
            PushState();
            Int16 val = ReadInt16();
            PopState();
            return val;
        }
        public Int32 PeekInt32()
        {
            PushState();
            Int32 val = ReadInt32();
            PopState();
            return val;
        }
        public Int64 PeekInt64()
        {
            PushState();
            Int64 val = ReadInt64();
            PopState();
            return val;
        }
        public Int16 ReadInt16()
        {
            byte[] buf = ReadBytes(2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToInt16(buf, 0);
        }
        public Int32 ReadInt32()
        {
            byte[] buf = ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToInt32(buf, 0);
        }
        public Int64 ReadInt64()
        {
            byte[] buf = ReadBytes(8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buf);
            return BitConverter.ToInt64(buf, 0);
        }
        public UInt16 PeekU16LE()
        {
            PushState();
            UInt16 val = ReadU16LE();
            PopState();
            return val;
        }

        public UInt32 ReadU32LE()
        {
            return ReadU32LE(null);
        }

        public UInt32 ReadU32LE(List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(4);

            if (slicesRead != null)
            {
                slicesRead.Clear();
                slicesRead.AddRange(lastReadSlices);
            }

            return (UInt32)buf[3] << 24 |
                   (UInt32)buf[2] << 16 |
                   (UInt32)buf[1] << 8 |
                   (UInt32)buf[0];
        }

        public UInt32 ReadU32BE(List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(4);

            if (slicesRead != null)
            {
                slicesRead.Clear();
                slicesRead.AddRange(lastReadSlices);
            }

            return (UInt32)buf[0] << 24 |
                   (UInt32)buf[1] << 16 |
                   (UInt32)buf[2] << 8 |
                   (UInt32)buf[3];
        }

        public UInt64 ReadU64LE(List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(8);

            if (slicesRead != null)
            {
                slicesRead.Clear();
                slicesRead.AddRange(lastReadSlices);
            }

            return (UInt32)buf[7] << 56 |
                   (UInt32)buf[6] << 48 |
                   (UInt32)buf[5] << 40 |
                   (UInt32)buf[4] << 32 |
                   (UInt32)buf[3] << 24 |
                   (UInt32)buf[2] << 16 |
                   (UInt32)buf[1] << 8 |
                   (UInt32)buf[0];
        }

        public UInt32 PeekU32LE()
        {
            PushState();
            UInt32 val = ReadU32LE();
            PopState();
            return val;
        }

        public string PeekStringASCII(int size)
        {
            PushState();
            string str = ReadStringASCII(size);
            PopState();
            return str;
        }

        public string PeekStringUTF8(int size)
        {
            PushState();
            string str = ReadStringUTF8(size);
            PopState();
            return str;
        }

        public string ReadStringASCII(int size)
        {
            return ReadStringASCII(size, null);
        }

        public string ReadStringASCII(int size, List<PacketSlice> slices)
        {
            byte[] bytes = ReadBytes(size, slices);
            return StaticUtils.DecodeASCII(bytes);
        }

        public string ReadStringUTF8(int size)
        {
            return ReadStringUTF8(size, null);
        }

        public string ReadStringUTF8(int size, List<PacketSlice> slices)
        {
            byte[] bytes = ReadBytes(size, slices);
            return StaticUtils.DecodeUTF8(bytes);
        }

        public string ReadRAPIString()
        {
            return ReadRAPIString(null);
        }

        public string ReadRAPIString(List<PacketSlice> slicesRead)
        {
            PushState();

            try
            {
                UInt32 size = ReadU32LE();

                if (slicesRead != null)
                {
                    slicesRead.Clear();
                    slicesRead.AddRange(lastReadSlices);
                }

                string str;
                if (size > 0)
                {
                    byte[] buf = ReadBytes((int)size);

                    if (slicesRead != null)
                        slicesRead.AddRange(lastReadSlices);

                    str = Encoding.Unicode.GetString(buf, 0, buf.Length - 2);
                }
                else
                {
                    str = null;
                }

                return str;
            }
            catch (EndOfStreamException e)
            {
                PopState();
                throw e;
            }
        }

        public string ReadCStringASCII(int size)
        {
            return ReadCStringASCII(size, null);
        }

        public string ReadCStringASCII(int size, List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(size, slicesRead);

            int len = -1;
            for (int i = 0; i < size; i++)
            {
                if (buf[i] == 0)
                {
                    len = i;
                    break;
                }
            }

            if (len == -1)
                throw new FormatException("string not zero-terminated");

            if (len > 0)
                return Encoding.ASCII.GetString(buf, 0, len);
            else
                return null;
        }

        public string ReadCStringUnicode(int size)
        {
            return ReadCStringUnicode(size, null);
        }

        public string ReadCStringUnicode(int size, List<PacketSlice> slicesRead)
        {
            byte[] buf = ReadBytes(size, slicesRead);

            int len = -1;
            for (int i = 0; i < size; i += 2)
            {
                if (buf[i] == 0 && buf[i + 1] == 0)
                {
                    len = i;
                    break;
                }
            }

            if (len == -1)
                throw new FormatException("string not zero-terminated");

            if (len > 0)
                return Encoding.Unicode.GetString(buf, 0, len);
            else
                return null;
        }

        public byte[] ReadBytes(int length)
        {
            return ReadBytes(length, null);
        }

        public byte[] ReadBytes(int length, List<PacketSlice> slicesRead)
        {
            byte[] buf = new byte[length];
            int n = Read(buf, 0, length);

            if (slicesRead != null)
            {
                slicesRead.Clear();
                slicesRead.AddRange(lastReadSlices);
            }

            if (n != length)
            {
                if (n > 0)
                {
                    Seek(-n, SeekOrigin.Current);
                }

                throw new EndOfStreamException(
                    String.Format("read {0} out of {1} byte{2} from stream",
                                  n, length, (length > 1) ? "s" : ""));
            }

            return buf;
        }

        public byte[] PeekBytes(int length)
        {
            PushState();
            byte[] buf = ReadBytes(length);
            PopState();
            return buf;
        }

        public string ReadLineUTF8()
        {
            return ReadLineUTF8(null, null);
        }

        public string ReadLineUTF8(List<PacketSlice> lineSlicesRead)
        {
            return ReadLineUTF8(lineSlicesRead, null);
        }

        public string ReadLineUTF8(List<PacketSlice> lineSlicesRead,
                                   List<PacketSlice> crlfSlicesRead)
        {
            PushState();

            string line = "";
            int pos = -1;

            try
            {
                while (pos < 0)
                {
                    int n = 128;
                    int available = (int)GetBytesAvailable();
                    if (n > available)
                        n = available;

                    if (n == 0)
                        throw new EndOfStreamException("newline not found");

                    line += ReadStringASCII(n);

                    pos = line.IndexOf('\n');
                }
            }
            finally
            {
                PopState();
            }

            bool foundCR = (pos > 0 && line[pos - 1] == '\r') ? true : false;

            byte[] bytes = ReadBytes((foundCR) ? pos - 1 : pos, lineSlicesRead);
            ReadBytes((foundCR) ? 2 : 1, crlfSlicesRead);

            return StaticUtils.DecodeUTF8(bytes);
        }

        public string PeekLineUTF8()
        {
            PushState();
            string str = ReadLineUTF8();
            PopState();
            return str;
        }
    }
}
