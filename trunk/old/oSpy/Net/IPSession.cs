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
using oSpy.Event;
using oSpy.Parser;


namespace oSpy.Net
{
    [Serializable()]
    public class IPSession
    {
        public event PacketParser.NewTransactionNodeHandler NewTransactionNode;

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
}
