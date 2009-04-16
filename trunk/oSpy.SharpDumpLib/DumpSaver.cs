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
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Xml;

namespace oSpy.SharpDumpLib
{
    public delegate void SaveCompletedEventHandler (object sender, SaveCompletedEventArgs e);

    public class DumpSaver : AsyncWorker
    {
        #region Events

        public event ProgressChangedEventHandler SaveProgressChanged;
        public event SaveCompletedEventHandler SaveCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler (Dump dump, Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler worker_delegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpSaver ()
            : base ()
        {
        }

        public DumpSaver (IContainer container)
            : base (container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual void Save (Dump dump, Stream stream)
        {
            DoSave (dump, stream, null);
        }

        public virtual void SaveAsync (Dump dump, Stream stream, object taskId)
        {
            AsyncOperation async_op = CreateOperation (taskId);

            worker_delegate = new WorkerEventHandler (SaveWorker);
            worker_delegate.BeginInvoke (dump, stream, async_op, completion_method_delegate, null, null);
        }

        public virtual void SaveAsyncCancel (object taskId)
        {
            CancelOperation (taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void SaveWorker (Dump dump, Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Exception e = null;

            try
            {
                DoSave (dump, stream, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            SaveState save_state = new SaveState (dump, stream, e, asyncOp);

            try { completionMethodDelegate (save_state); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs (object userSuppliedState)
        {
            return new SaveCompletedEventArgs (null, null, null, true, userSuppliedState);
        }

        protected override void ReportProgress (object e)
        {
            OnSaveProgressChanged (e as ProgressChangedEventArgs);
        }

        protected virtual void OnSaveProgressChanged (ProgressChangedEventArgs e)
        {
            if (SaveProgressChanged != null)
                SaveProgressChanged (this, e);
        }

        protected override void ReportCompletion (object e)
        {
            OnSaveCompleted (e as SaveCompletedEventArgs);
        }

        protected virtual void OnSaveCompleted (SaveCompletedEventArgs e)
        {
            if (SaveCompleted != null)
                SaveCompleted (this, e);
        }

        protected override void CompletionMethod (object state)
        {
            SaveState save_state = state as SaveState;

            AsyncOperation async_op = save_state.async_op;
            SaveCompletedEventArgs e = new SaveCompletedEventArgs (save_state.dump, save_state.stream, save_state.ex, false, async_op.UserSuppliedState);
            FinalizeOperation (async_op, e);
        }

        #endregion // Async glue

        #region Core implementation

        private void DoSave (Dump dump, Stream stream, AsyncOperation asyncOp)
        {
            BinaryWriter binWriter = new BinaryWriter (stream, Encoding.ASCII);

            char[] magic = "oSpy".ToCharArray ();
            uint version = 2;
            uint is_compressed = 0;
            uint num_events = (uint) dump.Events.Count;

            binWriter.Write (magic);
            binWriter.Write (version);
            binWriter.Write (is_compressed);
            binWriter.Write (num_events);
            binWriter.Flush ();
            binWriter = null;

            XmlTextWriter xmlWriter = new XmlTextWriter (stream, Encoding.UTF8);
            xmlWriter.WriteStartDocument (true);
            xmlWriter.WriteStartElement ("events");

            int n = 0;
            int numEvents = dump.Events.Count;
            foreach (Event ev in dump.Events.Values)
            {
                if (asyncOp != null)
                {
                    int pctComplete = (int)(((float)(n + 1) / (float)numEvents) * 100.0f);
                    ProgressChangedEventArgs e = new ProgressChangedEventArgs (pctComplete, asyncOp.UserSuppliedState);
                    asyncOp.Post (on_progress_report_delegate, e);
                }

                xmlWriter.WriteRaw (ev.RawData);

                n++;
            }

            xmlWriter.WriteEndElement ();
            xmlWriter.Flush ();
        }

        #endregion // Core implementation
    }

    #region Helper classes

    internal class SaveState
    {
        public Dump dump = null;
        public Stream stream = null;
        public Exception ex = null;
        public AsyncOperation async_op = null;

        public SaveState (Dump dump, Stream stream, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.stream = stream;
            this.ex = ex;
            this.async_op = asyncOp;
        }
    }

    public class SaveCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump = null;
        private Stream stream = null;

        public SaveCompletedEventArgs (Dump dump, Stream stream, Exception e, bool cancelled, object state)
            : base (e, cancelled, state)
        {
            this.dump = dump;
            this.stream = stream;
        }

        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary ();
                return dump;
            }
        }

        public Stream Stream
        {
            get
            {
                RaiseExceptionIfNecessary ();
                return stream;
            }
        }
    }

    #endregion // Helper classes
}
