using System;
using System.Collections.Generic;
using System.Text;
using Flobbster.Windows.Forms;
using System.IO;
using oSpy.Util;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Windows.Forms;
namespace oSpy.Parser
{
    //
    // Class that parses packets into transactions by using transaction
    // factories. It has a list of the raw packets as well as parsed
    // transactions.
    //
    [Serializable()]
    [TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public class PacketParser : PropertyTable, ISerializable
    {
        protected DebugLogger logger;
        protected List<TransactionFactory> factories;

        protected List<IPSession> sessions;
        protected Dictionary<int, IPPacket> packets;
        protected Dictionary<int, List<TransactionNode>> packetIndexToNodes;
        protected List<TransactionNode> nodes;

        public PacketParser(DebugLogger logger)
        {
            this.logger = logger;

            Initialize();
        }

        public PacketParser(SerializationInfo info, StreamingContext ctx)
        {
            Initialize();

            sessions = (List<IPSession>)info.GetValue("sessions", sessions.GetType());
            packets = (Dictionary<int, IPPacket>)info.GetValue("packets", packets.GetType());
            packetIndexToNodes = (Dictionary<int, List<TransactionNode>>)info.GetValue("packetIndexToNodes", packetIndexToNodes.GetType());
            nodes = (List<TransactionNode>)info.GetValue("nodes", nodes.GetType());
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            info.AddValue("sessions", sessions);
            info.AddValue("packets", packets);
            info.AddValue("packetIndexToNodes", packetIndexToNodes);
            info.AddValue("nodes", nodes);
        }

        private void Initialize()
        {
            factories = new List<TransactionFactory>(3);

            TransactionFactory fac;/* = new RAPITransactionFactory(logger);
            factories.Add(fac);

            fac = new RRACTransactionFactory(logger);
            factories.Add(fac);

            fac = new RemSyncTransactionFactory(logger);
            factories.Add(fac);*/

            fac = new HTTPTransactionFactory(logger);
            factories.Add(fac);

            fac = new MSNTransactionFactory(logger);
            factories.Add(fac);

            fac = new OracleTransaction(logger);
            factories.Add(fac);

            sessions = new List<IPSession>();
            packets = new Dictionary<int, IPPacket>(32);
            packetIndexToNodes = new Dictionary<int, List<TransactionNode>>(32);
            nodes = new List<TransactionNode>(32);
        }

        public List<TransactionFactory> Factories
        {
            get
            {
                return factories;
            }
        }
        private void session_NewTransactionNode(TransactionNode node)
        {
            string id = Convert.ToString(nodes.Count);

            PropertySpec propSpec = new PropertySpec(id, node.GetType(), "Packet");
            propSpec.Description = node.Name;
            propSpec.Attributes = new Attribute[1] { new ReadOnlyAttribute(true) };
            Properties.Add(propSpec);
            this[id] = node;

            nodes.Add(node);

            List<IPPacket> packets = new List<IPPacket>();
            foreach (PacketSlice slice in node.GetAllSlices())
            {
                if (!packetIndexToNodes.ContainsKey(slice.Packet.Index))
                    packetIndexToNodes[slice.Packet.Index] = new List<TransactionNode>(1);

                packetIndexToNodes[slice.Packet.Index].Add(node);

                if (!packets.Contains(slice.Packet))
                    packets.Add(slice.Packet);
            }

            PacketDescriptionReceived(packets.ToArray(),
                (node.Description.Length > 0) ? node.Description : node.Name);
        }

        public void AddSession(IPSession session)
        {
            sessions.Add(session);

            session.NewTransactionNode += session_NewTransactionNode;

            foreach (IPPacket p in session.StreamIn.Packets)
            {
                packets[p.Index] = p;
            }

            foreach (IPPacket p in session.StreamOut.Packets)
            {
                packets[p.Index] = p;
            }

            foreach (TransactionFactory fac in factories)
            {
                try
                {
                    if (fac.HandleSession(session))
                        break;
                }
                catch (Exception e)
                {
                    string msg = String.Format("An unhandled exception occured while the parser {0} was parsing session {1}: {2}\n\nBacktrace:\n{3}\n\nPlease submit your trace to oleavr@gmail.com",
                        fac.GetType().Name, session, e.Message, e.StackTrace);
                    MessageBox.Show(msg, "Parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw e;
                }

                session.ResetState();
            }

            session.AllNodesAdded();
        }

        public IPSession[] GetSessions()
        {
            return sessions.ToArray();
        }

        public void Reset()
        {
            sessions.Clear();
            packets.Clear();
            packetIndexToNodes.Clear();
            nodes.Clear();
        }

        public IPPacket GetPacket(int index)
        {
            return packets[index];
        }

        public TransactionNodeList GetTransactionsForPackets(List<IPPacket> packets)
        {
            TransactionNodeList list = new TransactionNodeList();

            List<TransactionNode> potentialNodes = new List<TransactionNode>(packets.Count);

            foreach (IPPacket packet in packets)
            {
                if (packetIndexToNodes.ContainsKey(packet.Index))
                {
                    potentialNodes.AddRange(packetIndexToNodes[packet.Index]);
                }
            }

            foreach (TransactionNode node in potentialNodes)
            {
                if (list.Contains(node))
                    continue;

                bool hasAll = true;
                foreach (PacketSlice slice in node.GetAllSlices())
                {
                    if (!packets.Contains(slice.Packet))
                    {
                        hasAll = false;
                        break;
                    }
                }

                if (hasAll)
                    list.PushNode(node);
            }

            list.PushedAllNodes();

            return list;
        }

        public event PacketDescriptionReceivedHandler PacketDescriptionReceived;
    }

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
