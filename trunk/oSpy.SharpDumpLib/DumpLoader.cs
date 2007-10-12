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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace oSpy.SharpDumpLib
{
    public delegate void LoadCompletedEventHandler(object sender, LoadCompletedEventArgs e);

    public class DumpLoader : AsyncWorker
    {
        #region Events

        public event ProgressChangedEventHandler LoadProgressChanged;
        public event LoadCompletedEventHandler LoadCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler(Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpLoader()
            : base()
        {
        }

        public DumpLoader(IContainer container)
            : base(container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual Dump Load(Stream stream)
        {
            return DoLoad(stream, null);
        }

        public virtual void LoadAsync(Stream stream, object taskId)
        {
            AsyncOperation asyncOp = CreateOperation(taskId);

            workerDelegate = new WorkerEventHandler(LoadWorker);
            workerDelegate.BeginInvoke(stream, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void LoadAsyncCancel(object taskId)
        {
            CancelOperation(taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void LoadWorker(Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Dump dump = null;
            Exception e = null;

            try
            {
                dump = DoLoad(stream, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            LoadState loadState = new LoadState(dump, e, asyncOp);

            try { completionMethodDelegate(loadState); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs(object userSuppliedState)
        {
            return new LoadCompletedEventArgs(null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnLoadProgressChanged(e as ProgressChangedEventArgs);
        }

        protected virtual void OnLoadProgressChanged(ProgressChangedEventArgs e)
        {
            if (LoadProgressChanged != null)
                LoadProgressChanged(this, e);
        }

        protected override void ReportCompletion(object e)
        {
            OnLoadCompleted(e as LoadCompletedEventArgs);
        }

        protected virtual void OnLoadCompleted(LoadCompletedEventArgs e)
        {
            if (LoadCompleted != null)
                LoadCompleted(this, e);
        }

        protected override void CompletionMethod(object state)
        {
            LoadState loadState = state as LoadState;

            AsyncOperation asyncOp = loadState.asyncOp;
            LoadCompletedEventArgs e = new LoadCompletedEventArgs(loadState.dump, loadState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private Dump DoLoad(Stream stream, AsyncOperation asyncOp)
        {
            Dump dump = new Dump();

            try
            {
                BinaryReader reader = new BinaryReader(stream);
                uint numEvents = reader.ReadUInt32();

                XmlTextReader xmlReader = new XmlTextReader(stream);

                uint n;
                for (n = 0; xmlReader.Read() && n < numEvents; n++)
                {
                    if (asyncOp != null)
                    {
                        int pctComplete = (int)(((float)(n + 1) / (float)numEvents) * 100.0f);
                        ProgressChangedEventArgs e = new ProgressChangedEventArgs(pctComplete, asyncOp.UserSuppliedState);
                        asyncOp.Post(onProgressReportDelegate, e);
                    }

                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "event")
                    {
                        XmlReader rdr = xmlReader.ReadSubtree();
                        XmlDocument doc = new XmlDocument();
                        doc.Load(rdr);
                        dump.AddEvent(doc.DocumentElement);
                    }
                }

                if (n != numEvents)
                    throw new InvalidDataException(String.Format("expected {0} events, read {1}", numEvents, n));
            }
            catch (Exception ex)
            {
                dump.Close();
                throw ex;
            }

            return dump;
        }

        #endregion // Core implementation
    }

    #region Helper classes

    internal class LoadState
    {
        public Dump dump = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public LoadState(Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class LoadCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump = null;
        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary();
                return dump;
            }
        }

        public LoadCompletedEventArgs(Dump dump, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
        }
    }

    #endregion // Helper classes
}
