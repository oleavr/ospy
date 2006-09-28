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
using Flobbster.Windows.Forms;
using System.IO;
using oSpy.Util;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Windows.Forms;
using oSpy.Parser;
namespace oSpy.Net
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

}
