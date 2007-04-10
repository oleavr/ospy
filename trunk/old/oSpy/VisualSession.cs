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
using System.IO;
using oSpy.Parser;
using oSpy.Event;
using oSpy.Net;
namespace oSpy
{
    [Serializable()]
    public class VisualSession
    {
        private IPEndpoint localEndpoint;
        public IPEndpoint LocalEndpoint
        {
            get { return localEndpoint; }
        }

        private IPEndpoint remoteEndpoint;
        public IPEndpoint RemoteEndpoint
        {
            get { return remoteEndpoint; }
        }

        private List<VisualTransaction> transactions;
        public List<VisualTransaction> Transactions
        {
            get { return transactions; }
        }

        public VisualSession(IPSession session)
        {
            localEndpoint = session.LocalEndpoint;
            remoteEndpoint = session.RemoteEndpoint;
            transactions = new List<VisualTransaction>();
        }

        public void TransactionsCreated()
        {
            foreach (VisualTransaction transaction in transactions)
            {
                transaction.TransactionsCreated();
            }
        }

        public void SessionsCreated()
        {
            foreach (VisualTransaction transaction in transactions)
            {
                transaction.SessionsCreated();
            }
        }
    }

    public abstract class SessionVisualizer
    {
        public virtual bool Visible
        {
            get { return true; }
        }

        public virtual string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }

        public abstract VisualTransaction[] GetTransactions(IPSession session);

        public override string ToString()
        {
            return Name;
        }
    }

    public class TCPEventsVisualizer : SessionVisualizer
    {
        public override string Name
        {
            get { return "TCPEvents"; }
        }

        public override bool Visible
        {
            get { return false; }
        }

        public override VisualTransaction[] GetTransactions(IPSession session)
        {
            List<VisualTransaction> transactions = new List<VisualTransaction>(session.Events.Count);

            foreach (TCPEvent ev in session.Events)
            {
                VisualTransaction transaction = new VisualTransaction(0, ev.Timestamp);
                transaction.HeadlineText = ev.ToString();
                transactions.Add(transaction);
            }

            return transactions.ToArray();
        }
    }

    public class TCPVisualizer : SessionVisualizer
    {
        public override string Name
        {
            get { return "TCP"; }
        }

        public override VisualTransaction[] GetTransactions(IPSession session)
        {
            List<VisualTransaction> transactions = new List<VisualTransaction>();

            session.ResetState();

            PacketStream stream1 = session.GetNextStreamDirection();
            PacketStream stream2 = session.GetNextStreamDirection();

            List<IPPacket> packets = new List<IPPacket>(stream1.Packets.Count + stream2.Packets.Count);
            packets.AddRange(stream1.Packets);
            packets.AddRange(stream2.Packets);
            packets.Sort();

            VisualTransaction transaction = null;
            MemoryStream previewData = new MemoryStream();
            int transferred = 0;

            foreach (IPPacket packet in packets)
            {
                if (transaction == null || (transaction != null && packet.Direction != transaction.Direction))
                {
                    if (transaction != null)
                    {
                        transaction.AddHeaderField("Size", transferred);

                        byte[] bytes = previewData.ToArray();
                        transaction.SetBodyFromPreviewData(bytes, bytes.Length);
                    }

                    transaction = new VisualTransaction(packet.Index, packet.Direction, packet.Timestamp);
                    transaction.AddHeaderField("Direction", packet.Direction);
                    transaction.HeaderRowsPerCol = 1;
                    transactions.Add(transaction);
                    previewData.SetLength(0);
                    transferred = 0;
                }

                transaction.EndTime = packet.Timestamp;

                previewData.Write(packet.Bytes, 0, packet.Bytes.Length);

                transferred += packet.Bytes.Length;
            }

            if (transaction != null)
            {
                transaction.AddHeaderField("Size", transferred);

                byte[] bytes = previewData.ToArray();
                transaction.SetBodyFromTruncatedPreviewData(bytes, transferred - bytes.Length);
            }

            return transactions.ToArray();
        }
    }

    public class StreamVisualizationManager
    {
        protected static List<SessionVisualizer> visualizers;
        protected static Dictionary<string, SessionVisualizer> visualizerFromName;
        public static SessionVisualizer[] Visualizers
        {
            get { return visualizers.ToArray(); }
        }

        protected static SessionVisualizer tcpEventsVis;
        public static SessionVisualizer TCPEventsVis
        {
            get { return tcpEventsVis; }
        }

        static StreamVisualizationManager()
        {
            // FIXME: get these from plugins

            visualizers = new List<SessionVisualizer>();
            visualizerFromName = new Dictionary<string, SessionVisualizer>();

            tcpEventsVis = new TCPEventsVisualizer();
            Register(tcpEventsVis);

            Register(new TCPVisualizer());
            Register(new HTTPVisualizer());
            Register(new MSNSBVisualizer());
            Register(new MSNP2PVisualizer());
        }

        private static void Register(SessionVisualizer visualizer)
        {
            visualizers.Add(visualizer);
            visualizerFromName.Add(visualizer.Name, visualizer);
        }
    }
}
