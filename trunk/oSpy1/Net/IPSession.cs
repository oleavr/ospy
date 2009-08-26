//
// Copyright (c) 2006-2009 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Text;
using oSpy.Event;
using oSpy.Parser;
using System.Net;

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

        public IPEndPoint LocalEndpoint
        {
            get
            {
                if (streamIn.Packets.Count > 0)
                    return streamIn.Packets[0].LocalEndpoint;
                else
                    return streamOut.Packets[0].LocalEndpoint;
            }
        }

        public IPEndPoint RemoteEndpoint
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

        public static IPSession[] ExtractAllFrom(IPPacket[] packets, TCPEvent[] events, IProgressFeedback progress)
        {
            //
            // Find sessions
            //
            progress.ProgressUpdate("Identifying sessions", 0);

            Dictionary<UInt32, IPSession> sessionById = new Dictionary<UInt32, IPSession>();
            List<UInt32> sessionIds = new List<UInt32>();

            int i = 0;
            foreach (IPPacket p in packets)
            {
                UInt32 id = p.ResourceId;

                IPSession session;

                if (sessionById.ContainsKey(id))
                {
                    session = sessionById[id];
                }
                else
                {
                    session = new IPSession();
                    sessionById[id] = session;
                    sessionIds.Add(id);
                }

                session.AddPacket(p);
                i++;

                progress.ProgressUpdate("Identifying sessions",
                    (int) ((i / (float) packets.Length) * 100.0f));
            }

            //
            // Add the events to the appropriate sessions
            //

            // First off, sort them by timestamp
            Array.Sort(events);

            // Then match them with the sessions
            foreach (TCPEvent ev in events)
            {
                if (sessionById.ContainsKey(ev.ResourceId))
                    sessionById[ev.ResourceId].AddEvent(ev);
            }

            //
            // Build an ordered list of sessions
            //
            List<IPSession> result = new List<IPSession>();
            foreach (UInt32 id in sessionIds)
                result.Add(sessionById[id]);

            return result.ToArray();
        }
    }
}
