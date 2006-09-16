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
using System.ComponentModel;
using Flobbster.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace oSpy
{
    public interface DebugLogger
    {
        void AddMessage(string msg);
    }

    public delegate void PacketDescriptionReceivedHandler(IPPacket[] packets, string description);

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
            packets = (Dictionary<int, IPPacket>) info.GetValue("packets", packets.GetType());
            packetIndexToNodes = (Dictionary<int, List<TransactionNode>>) info.GetValue("packetIndexToNodes", packetIndexToNodes.GetType());
            nodes = (List<TransactionNode>) info.GetValue("nodes", nodes.GetType());
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

            sessions = new List<IPSession>();
            packets = new Dictionary<int, IPPacket>(32);
            packetIndexToNodes = new Dictionary<int, List<TransactionNode>>(32);
            nodes = new List<TransactionNode>(32);
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

    //
    // Base class for all transaction factories
    //
    class ProtocolError: Exception { };

    public abstract class TransactionFactory
    {
        protected DebugLogger logger;
        public DebugLogger Logger
        {
            get { return logger; }
        }

        public TransactionFactory(DebugLogger logger)
        {
            this.logger = logger;
        }

        public abstract bool HandleSession(IPSession session);
    }

    //
    // Transaction node
    //
    [Serializable()]
    [TypeConverterAttribute(typeof(TransactionNodeConverter))]
    public class TransactionNode : PropertyTable, IComparable
    {
        protected TransactionNode parent;
        public TransactionNode Parent
        {
            get { return parent; }
        }

        protected List<TransactionNode> children;
        public List<TransactionNode> Children
        {
            get { return children; }
        }

        protected Dictionary<string, TransactionNode> childrenDict;
        public Dictionary<string, TransactionNode> ChildrenDict
        {
            get { return childrenDict; }
        }

        protected string name;
        public string Name
        {
            get { return name; }
        }

        protected string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        protected string summary;
        public string Summary
        {
            get { return summary; }
            set { summary = value; }
        }

        protected Dictionary<string, object> fields;
        public Dictionary<string, object> Fields
        {
            get { return fields; }
        }

        protected List<PacketSlice> slices;
        public List<PacketSlice> Slices
        {
            get { return slices; }
        }

        protected Dictionary<string, List<PacketSlice>> fieldSlices;

        public PacketSlice FirstSlice
        {
            get
            {
                if (slices.Count > 0)
                    return slices[0];
                else if (children.Count > 0)
                    return children[0].FirstSlice;
                else
                    return null;
            }
        }

        public PacketSlice LastSlice
        {
            get
            {
                if (children.Count > 0)
                    return children[children.Count - 1].LastSlice;
                else if (slices.Count > 0)
                    return slices[slices.Count - 1];
                else
                    return null;
            }
        }

        public DateTime StartTime
        {
            get { return FirstSlice.Packet.Timestamp; }
        }

        public DateTime EndTime
        {
            get { return LastSlice.Packet.Timestamp; }
        }

        public int Index
        {
            get
            {
                PacketSlice slice = FirstSlice;
                return (slice.Packet.Index << 11) | slice.Offset;
            }
        }

        public TransactionNode(string name)
        {
            Initialize(null, name);
        }

        public TransactionNode(TransactionNode parent, string name)
        {
            Initialize(parent, name);

            parent.AddChild(this);
        }

        private void Initialize(TransactionNode parent, string name)
        {
            this.name = name;
            this.description = "";
            this.summary = "";
            this.parent = parent;
            this.children = new List<TransactionNode>();
            this.childrenDict = new Dictionary<string, TransactionNode>();
            this.slices = new List<PacketSlice>();
            this.fields = new Dictionary<string, object>();
            this.fieldSlices = new Dictionary<string, List<PacketSlice>>();
        }

        public void AddChild(TransactionNode node)
        {
            children.Add(node);
            childrenDict[node.Name] = node;

            PropertySpec propSpec = new PropertySpec(node.Name, typeof(TransactionNode), "Packet");
            propSpec.Description = node.Name;
            propSpec.Attributes = new Attribute[1] { new ReadOnlyAttribute(true) };
            Properties.Add(propSpec);
            this[node.Name] = node;
        }

        public TransactionNode FindChild(string name)
        {
            if (childrenDict.ContainsKey(name))
                return childrenDict[name];

            foreach (TransactionNode child in childrenDict.Values)
            {
                TransactionNode node = child.FindChild(name);
                if (node != null)
                    return node;
            }

            return null;
        }

        public void AddField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddField(name, value, value, description, slices);
        }

        public void AddField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            this.fields[name] = value;
            Properties.Add(new PropertySpec(name, typeof(ValueType), "Packet", description));
            this[name] = formattedValue;
            AddFieldSlices(name, slices);
        }

        public void AddTextField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddTextField(name, value, value, description, slices);
        }

        public void AddTextField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            AddSpecialField(name, value, formattedValue, description, slices, typeof(TextUITypeEditor));
        }

        public void AddXMLField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddXMLField(name, value, value, description, slices);
        }

        public void AddXMLField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            AddSpecialField(name, value, formattedValue, description, slices, typeof(XMLUITypeEditor));
        }

        protected void AddSpecialField(string name, object value, object formattedValue, string description, List<PacketSlice> slices, Type editorType)
        {
            this.fields[name] = value;
            PropertySpec propSpec = new PropertySpec(name, typeof(string), "Packet");
            propSpec.Description = description;
            propSpec.Attributes = new Attribute[2] {
                    new System.ComponentModel.EditorAttribute(editorType, typeof(System.Drawing.Design.UITypeEditor)),
                    new ReadOnlyAttribute(true)
                };
            Properties.Add(propSpec);
            this[name] = formattedValue;
            AddFieldSlices(name, slices);
        }

        protected void AddFieldSlices(string name, List<PacketSlice> slices)
        {
            this.slices.AddRange(slices);
            fieldSlices[name] = new List<PacketSlice>(slices);
            slices.Clear();
        }

        public List<PacketSlice> GetAllSlices()
        {
            List<PacketSlice> all = new List<PacketSlice>(slices.Count);
            all.AddRange(slices);

            foreach (TransactionNode node in childrenDict.Values)
            {
                List<PacketSlice> childAll = node.GetAllSlices();
                all.AddRange(childAll);
            }

            return all;
        }

        //
        // Gets the slices for the given path, which may contain the
        // full pipe-separated path to a subnode's field, or just the
        // name of a field on this node.
        //
        // For example:
        //   "1|Request|hKey"
        //
        public List<PacketSlice> GetSlicesForFieldPath(string path)
        {
            if (path == null || path == "")
                return GetAllSlices();

            if (path.IndexOf("|") == -1)
            {
                if (childrenDict.ContainsKey(path))
                    return childrenDict[path].GetAllSlices();
                else
                    return fieldSlices[path];
            }
            
            string[] tokens = path.Split(new char[] { '|' }, 2);
            return childrenDict[tokens[0]].GetSlicesForFieldPath(tokens[1]);
        }

        public int CompareTo(Object obj)
        {
            TransactionNode otherNode = obj as TransactionNode;

            PacketSlice slice = FirstSlice;
            PacketSlice otherSlice = otherNode.FirstSlice;

            if (slice != null && otherSlice != null)
                return FirstSlice.CompareTo(otherNode.FirstSlice);
            else
                return 0; // FIXME
        }
    }

    class TransactionNodeConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(TransactionNode))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(System.String) &&
                value is TransactionNode)
            {
                TransactionNode node = value as TransactionNode;

                return node.Description;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [Serializable()]
    public class TransactionNodeList : PropertyTable
    {
        protected List<TransactionNode> pendingNodes;
        protected Dictionary<int, TransactionNode> nodes;

        public int Count
        {
            get { return nodes.Count; }
        }

        public TransactionNode this[int index]
        {
            get { return nodes[index]; }
        }

        public TransactionNodeList()
        {
            pendingNodes = new List<TransactionNode>();
            nodes = new Dictionary<int, TransactionNode>();
        }

        public void PushNode(TransactionNode node)
        {
            pendingNodes.Add(node);
        }

        public void PushedAllNodes()
        {
            pendingNodes.Sort();

            int i = 0;
            foreach (TransactionNode node in pendingNodes)
            {
                string name = Convert.ToString(i);
                Properties.Add(new PropertySpec(name, node.GetType(), "Node", ""));
                this[name] = node;
                nodes[i] = node;

                i++;
            }

            pendingNodes.Clear();
        }

        public bool Contains(TransactionNode node)
        {
            return (nodes.ContainsValue(node) || pendingNodes.Contains(node));
        }
    }

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

    public delegate void NewTransactionNodeHandler(TransactionNode node);

    [Serializable()]
    public class IPSession
    {
        public event NewTransactionNodeHandler NewTransactionNode;

        protected List<TCPEvent> events;
        public List<TCPEvent> Events
        {
            get { return events; }
        }

        protected PacketStream streamIn;
        public PacketStream StreamIn
        {
            get { return streamIn; }
        }

        protected PacketStream streamOut;
        public PacketStream StreamOut
        {
            get { return streamOut; }
        }

        protected PacketDirection curDirection;

        public IPEndpoint LocalEndpoint
        {
            get
            {
                if (streamIn.Packets.Count > 0)
                    return streamIn.Packets[0].LocalEndpoint;
                else
                    return streamOut.Packets[0].LocalEndpoint;
            }
        }

        public IPEndpoint RemoteEndpoint
        {
            get
            {
                if (streamIn.Packets.Count > 0)
                    return streamIn.Packets[0].RemoteEndpoint;
                else
                    return streamOut.Packets[0].RemoteEndpoint;
            }
        }

        public long TotalBytes
        {
            get { return streamIn.Length + streamOut.Length; }
        }

        public long BytesReceived
        {
            get { return streamIn.Length; }
        }

        public long BytesSent
        {
            get { return streamOut.Length; }
        }

        private List<TransactionNode> nodes;
        public List<TransactionNode> Nodes
        {
            get { return nodes; }
        }

        public IPSession()
        {
            events = new List<TCPEvent>();
            streamIn = new PacketStream();
            streamOut = new PacketStream();
            nodes = new List<TransactionNode>();
        }

        public void AddEvent(TCPEvent ev)
        {
            events.Add(ev);
        }

        public void AddPacket(IPPacket p)
        {
            if (p.Direction == PacketDirection.PACKET_DIRECTION_INCOMING)
                streamIn.AppendPacket(p);
            else
                streamOut.AppendPacket(p);
        }

        public void AddNode(TransactionNode node)
        {
            nodes.Add(node);

            if (NewTransactionNode != null)
                NewTransactionNode(node);
        }

        public void AllNodesAdded()
        {
            nodes.Sort();
        }

        public void ResetState()
        {
            curDirection = PacketDirection.PACKET_DIRECTION_INVALID;
            streamIn.ResetState();
            streamOut.ResetState();
        }

        public PacketStream GetNextStreamDirection()
        {
            if (curDirection == PacketDirection.PACKET_DIRECTION_INCOMING)
                curDirection = PacketDirection.PACKET_DIRECTION_OUTGOING;
            else if (curDirection == PacketDirection.PACKET_DIRECTION_OUTGOING)
                curDirection = PacketDirection.PACKET_DIRECTION_INCOMING;
            else if (curDirection == PacketDirection.PACKET_DIRECTION_INVALID)
            {
                IPPacket pktIn = null, pktOut = null;

                if (streamIn.Packets.Count > 0)
                {
                    pktIn = streamIn.Packets[0];

                    if (streamOut.Packets.Count > 0)
                    {
                        pktOut = streamOut.Packets[0];

                        // FIXME: is this check correct for the earliest packet?
                        if (pktIn.CompareTo(pktOut) < 0)
                            curDirection = PacketDirection.PACKET_DIRECTION_INCOMING;
                        else
                            curDirection = PacketDirection.PACKET_DIRECTION_OUTGOING;
                    }
                }

                if (curDirection == PacketDirection.PACKET_DIRECTION_INVALID)
                {
                    if (pktIn != null)
                        curDirection = PacketDirection.PACKET_DIRECTION_INCOMING;
                    else
                        curDirection = PacketDirection.PACKET_DIRECTION_OUTGOING;
                }
            }

            if (curDirection == PacketDirection.PACKET_DIRECTION_INCOMING)
                return streamIn;
            else
                return streamOut;
        }

        public override string ToString()
        {
            return String.Format("{0} <-> {1}", LocalEndpoint, RemoteEndpoint);
        }
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
                    int pktOffset = (int) position - offset;

                    curSlice = new PacketSlice(packet, pktOffset, (int) pktLen - pktOffset, i);
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

                lastReadSlices.Add(new PacketSlice(curSlice.Packet, curSlice.Offset, (int) n));

                Array.Copy(curSlice.Packet.Bytes, curSlice.Offset, buffer, dstOffset, n);

                curSlice.Offset += (int) n;
                curSlice.Length -= (int) n;

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

            return (int) totalBytesToRead;
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
                   (UInt32)buf[1] <<  8 |
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
                   (UInt32)buf[1] <<  8 |
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
            return Util.DecodeASCII(bytes);
        }

        public string ReadStringUTF8(int size)
        {
            return ReadStringUTF8(size, null);
        }

        public string ReadStringUTF8(int size, List<PacketSlice> slices)
        {
            byte[] bytes = ReadBytes(size, slices);
            return Util.DecodeUTF8(bytes);
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
                    byte[] buf = ReadBytes((int) size);

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

            return Util.DecodeUTF8(bytes);
        }

        public string PeekLineUTF8()
        {
            PushState();
            string str = ReadLineUTF8();
            PopState();
            return str;
        }
    }

    public enum SocketEventType
    {
        UNKNOWN,
        LISTENING,
        CONNECTED_INBOUND,
        CONNECTING,
        CONNECTED_OUTBOUND,
        DISCONNECTED,
        RESET,
    }

    [Serializable()]
    public class TCPEvent : IComparable
    {
        protected DateTime timestamp;
        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        protected UInt32 resourceId;
        public UInt32 ResourceId
        {
            get { return resourceId; }
        }

        protected SocketEventType type;
        public SocketEventType Type
        {
            get { return type; }
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

        public TCPEvent(DateTime timestamp, UInt32 resourceId, SocketEventType type,
                        IPEndpoint localEndpoint, IPEndpoint remoteEndpoint)
        {
            this.timestamp = timestamp;
            this.resourceId = resourceId;
            this.type = type;
            this.localEndpoint = localEndpoint;
            this.remoteEndpoint = remoteEndpoint;
        }

        public int CompareTo(Object obj)
        {
            TCPEvent otherEvent = obj as TCPEvent;

            return timestamp.CompareTo(otherEvent.timestamp);
        }

        public override string ToString()
        {
            string text;

            switch (type)
            {
                case SocketEventType.CONNECTED_INBOUND:
                    text = "Client connected";
                    break;
                case SocketEventType.CONNECTED_OUTBOUND:
                    text = "Connected";
                    break;
                case SocketEventType.CONNECTING:
                    text = "Connecting";
                    break;
                case SocketEventType.DISCONNECTED:
                    text = "Connection closed";
                    break;
                case SocketEventType.LISTENING:
                    text = "Listening for connections";
                    break;
                case SocketEventType.RESET:
                    text = "Connection reset";
                    break;
                default:
                    text = "Unknown event";
                    break;
            }

            return text;
        }
    }

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

    [Serializable()]
    public class IPEndpoint
    {
        protected string address;
        public string Address
        {
            get
            {
                return address;
            }
        }

        protected int port;
        public int Port
        {
            get
            {
                return port;
            }
        }

        public IPEndpoint(string address, int port)
        {
            this.address = LookupAddress(address);
            this.port = port;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", address, port);
        }

        protected static string LookupAddress(string addr)
        {
            if (addr == "169.254.2.1")
                return "PDA";
            else if (addr == "169.254.2.2")
                return "HOST";
            else
                return addr;
        }
    }

    public class Constants
    {
        public const int ERROR_SUCCESS = 0;

        public const uint REG_NONE = 0;
        public const uint REG_SZ = 1;
        public const uint REG_EXPAND_SZ = 2;
        public const uint REG_BINARY = 3;
        public const uint REG_DWORD = 4;
        public const uint REG_DWORD_BIG_ENDIAN = 5;
        public const uint REG_LINK = 6;
        public const uint REG_MULTI_SZ = 7;
        public const uint REG_RESOURCE_LIST = 8;

        public const int REG_CREATED_NEW_KEY = 0x1;
        public const int REG_OPENED_EXISTING_KEY = 0x2;
    }
}
