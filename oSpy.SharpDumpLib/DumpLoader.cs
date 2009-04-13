//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpy.SharpDumpLib
{
    public delegate void LoadCompletedEventHandler (object sender, LoadCompletedEventArgs e);

    public class DumpLoader : AsyncWorker
    {
        #region Events

        public event ProgressChangedEventHandler LoadProgressChanged;
        public event LoadCompletedEventHandler LoadCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler (Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler worker_delegate;

        private EventFactory event_factory = new EventFactory ();

        #endregion // Internal members

        #region Construction and destruction

        public DumpLoader ()
            : base ()
        {
        }

        public DumpLoader (IContainer container)
            : base (container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual Dump Load (Stream stream)
        {
            return DoLoad (stream, null);
        }

        public virtual void LoadAsync (Stream stream, object taskId)
        {
            AsyncOperation async_op = CreateOperation (taskId);

            worker_delegate = new WorkerEventHandler (LoadWorker);
            worker_delegate.BeginInvoke (stream, async_op, completion_method_delegate, null, null);
        }

        public virtual void LoadAsyncCancel (object taskId)
        {
            CancelOperation (taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void LoadWorker (Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Dump dump = null;
            Exception e = null;

            try {
                dump = DoLoad (stream, asyncOp);
            } catch (Exception ex) {
                e = ex;
            }

            LoadState loadState = new LoadState (dump, e, asyncOp);

            try { completionMethodDelegate (loadState); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs (object userSuppliedState)
        {
            return new LoadCompletedEventArgs (null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnLoadProgressChanged (e as ProgressChangedEventArgs);
        }

        protected virtual void OnLoadProgressChanged (ProgressChangedEventArgs e)
        {
            if (LoadProgressChanged != null)
                LoadProgressChanged (this, e);
        }

        protected override void ReportCompletion (object e)
        {
            OnLoadCompleted (e as LoadCompletedEventArgs);
        }

        protected virtual void OnLoadCompleted (LoadCompletedEventArgs e)
        {
            if (LoadCompleted != null)
                LoadCompleted (this, e);
        }

        protected override void CompletionMethod (object state)
        {
            LoadState load_state = state as LoadState;

            AsyncOperation async_op = load_state.async_op;
            LoadCompletedEventArgs e = new LoadCompletedEventArgs (load_state.dump, load_state.ex, false, async_op.UserSuppliedState);
            FinalizeOperation (async_op, e);
        }

        #endregion // Async glue

        #region Core implementation

        private Dump DoLoad (Stream stream, AsyncOperation asyncOp)
        {
            Dump dump = new Dump ();

            BinaryReader reader = new BinaryReader (stream, System.Text.Encoding.ASCII);

            string magic = new string (reader.ReadChars (4));
            uint version = reader.ReadUInt32 ();
            uint is_compressed = reader.ReadUInt32 ();
            uint num_events = reader.ReadUInt32 ();

            if (magic != "oSpy")
                throw new InvalidDataException ("invalid signature '" + magic + "'");
            else if (version != 2)
                throw new InvalidDataException ("unsupported version " + version);
            else if (is_compressed != 0 && is_compressed != 1)
                throw new InvalidDataException ("invalid value for is_compressed");

            //if (is_compressed == 1)
            //    stream = new BZip2InputStream (stream);

            XmlTextReader xml_reader = new XmlTextReader (stream);

            uint n;

            for (n = 0; xml_reader.Read () && n < num_events;) {
                if (asyncOp != null) {
                    int pct_complete = (int) (((float) (n + 1) / (float) num_events) * 100.0f);
                    ProgressChangedEventArgs e = new ProgressChangedEventArgs (pct_complete, asyncOp.UserSuppliedState);
                    asyncOp.Post (on_progress_report_delegate, e);
                }

                if (xml_reader.NodeType == XmlNodeType.Element && xml_reader.Name == "event") {
                    XmlReader rdr = xml_reader.ReadSubtree ();
                    Event ev = event_factory.CreateEvent (rdr);
                    dump.AddEvent (ev);

                    n++;
                }
            }

            if (n != num_events)
                throw new InvalidDataException (String.Format ("expected {0} events, read {1}", num_events, n));

            return dump;
        }

        #endregion // Core implementation
    }

    #region Helper classes

    internal class LoadState
    {
        public Dump dump;
        public Exception ex;
        public AsyncOperation async_op;

        public LoadState (Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.ex = ex;
            this.async_op = asyncOp;
        }
    }

    public class LoadCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump;
        public Dump Dump {
            get {
                RaiseExceptionIfNecessary ();
                return dump;
            }
        }

        public LoadCompletedEventArgs (Dump dump, Exception e, bool cancelled,
                                       object state)
            : base (e, cancelled, state)
        {
            this.dump = dump;
        }
    }

    #endregion // Helper classes
}
