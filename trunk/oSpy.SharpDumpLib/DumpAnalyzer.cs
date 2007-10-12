//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace oSpy.SharpDumpLib
{
    public delegate void AnalyzeProgressChangedEventHandler (object sender, ProgressChangedEventArgs e);
    public delegate void AnalyzeCompletedEventHandler (object sender, AnalyzeCompletedEventArgs e);

    public class DumpAnalyzer : AsyncWorker
    {
        #region Events

        public event AnalyzeProgressChangedEventHandler AnalyzeProgressChanged;
        public event AnalyzeCompletedEventHandler AnalyzeCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler (List<DataTransfer> dataTransfers, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpAnalyzer ()
            : base ()
        {
        }

        public DumpAnalyzer (IContainer container)
            : base (container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual List<ProtocolNode> Analyze (List<DataTransfer> transfers)
        {
            return DoAnalysis (transfers, null);
        }

        public virtual void AnalyzeAsync (List<DataTransfer> transfers, object taskId)
        {
            AsyncOperation asyncOp = CreateOperation (taskId);

            workerDelegate = new WorkerEventHandler (AnalyzeWorker);
            workerDelegate.BeginInvoke (transfers, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void AnalyzeAsyncCancel (object taskId)
        {
            CancelOperation (taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void AnalyzeWorker (List<DataTransfer> transfers, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            List<ProtocolNode> nodes = null;
            Exception e = null;

            try
            {
                nodes = DoAnalysis (transfers, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            AnalyzeDumpState parseState = new AnalyzeDumpState (transfers, nodes, e, asyncOp);

            try { completionMethodDelegate (parseState); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs (object userSuppliedState)
        {
            return new AnalyzeCompletedEventArgs (null, null, null, true, userSuppliedState);
        }

        protected override void ReportProgress (object e)
        {
            OnAnalyzeProgressChanged (e as ProgressChangedEventArgs);
        }

        protected virtual void OnAnalyzeProgressChanged (ProgressChangedEventArgs e)
        {
            if (AnalyzeProgressChanged != null)
                AnalyzeProgressChanged (this, e);
        }

        protected override void ReportCompletion (object e)
        {
            OnAnalyzeCompleted (e as AnalyzeCompletedEventArgs);
        }

        protected virtual void OnAnalyzeCompleted (AnalyzeCompletedEventArgs e)
        {
            if (AnalyzeCompleted != null)
                AnalyzeCompleted (this, e);
        }

        protected override void CompletionMethod (object state)
        {
            AnalyzeDumpState parseState = state as AnalyzeDumpState;

            AsyncOperation asyncOp = parseState.asyncOp;
            AnalyzeCompletedEventArgs e = new AnalyzeCompletedEventArgs (parseState.transfers, parseState.nodes, parseState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation (asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private List<ProtocolNode> DoAnalysis (List<DataTransfer> dataTransfers, AsyncOperation asyncOp)
        {
            List<ProtocolNode> nodes = new List<ProtocolNode> ();

            foreach (DataTransfer transfer in dataTransfers)
            {
                DataTransferStream stream = new DataTransferStream (transfer);


            }

            return null;
        }

        #endregion // Core implementation
    }

    public class HTTPProtocolParser : IProtocolParser
    {
        public string Name
        {
            get { return "HTTP"; }
        }

        public bool HandleStream (Stream stream, List<ProtocolNode> resultNodes, List<Stream> resultStreams)
        {
            ProtocolNode node = new ProtocolNode ();
            ProtocolReader reader = new ProtocolReader (stream);
            StreamSegment segment = reader.CreateSegment ();

            string line = segment.TryReadLine ();
            if (line == null)
                return false;

            string[] parts = line.Split (new char[] { ' ' });
            if (parts.Length != 3)
                return false;

            string verb = parts[0];
            if (!IsHttpVerb (verb))
                return false;

            node.AppendField ("Request", line, segment);

            ProtocolNode headerNode = new ProtocolNode ();

            do
            {
                segment = reader.CreateSegment ();

                line = segment.ReadLine ();
                if (line != String.Empty)
                {
                    parts = line.Split (new char[] { ':' }, 2);

                    headerNode.AppendField (parts[0].Trim (), parts[1].Trim (), segment);
                }
            } while (line != String.Empty);

            ProtocolNode bodyNode = new ProtocolNode ();


            return true;
        }

        private bool IsHttpVerb (string s)
        {
            return (s == "GET" || s == "POST"); // temporarily
        }
    }

    #region Helper classes

    internal class AnalyzeDumpState
    {
        public List<DataTransfer> transfers = null;
        public List<ProtocolNode> nodes = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public AnalyzeDumpState (List<DataTransfer> transfers, List<ProtocolNode> nodes, Exception ex, AsyncOperation asyncOp)
        {
            this.transfers = transfers;
            this.nodes = nodes;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class AnalyzeCompletedEventArgs : AsyncCompletedEventArgs
    {
        private List<DataTransfer> dataTransfers = null;
        public List<DataTransfer> DataTransfers
        {
            get
            {
                RaiseExceptionIfNecessary ();
                return dataTransfers;
            }
        }

        private List<ProtocolNode> protocolNodes = null;
        public List<ProtocolNode> ProtocolNodes
        {
            get
            {
                RaiseExceptionIfNecessary ();
                return protocolNodes;
            }
        }

        public AnalyzeCompletedEventArgs (List<DataTransfer> dataTransfers, List<ProtocolNode> protocolNodes, Exception e, bool cancelled, object state)
            : base (e, cancelled, state)
        {
            this.dataTransfers = dataTransfers;
            this.protocolNodes = protocolNodes;
        }
    }

    #endregion // Helper classes
}
