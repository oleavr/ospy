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
    public delegate void LoadCompletedEventHandler(object sender, LoadCompletedEventArgs e);

    public class DumpLoader : AsyncWorker
    {
        #region Internal members

        private WorkerEventHandler m_workerDelegate;
        private EventFactory m_eventFactory = new EventFactory();

        private delegate void WorkerEventHandler(Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);

        #endregion // Internal members

        #region Events

        public event ProgressChangedEventHandler LoadProgressChanged;
        public event LoadCompletedEventHandler LoadCompleted;

        #endregion // Events

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

            m_workerDelegate = new WorkerEventHandler(LoadWorker);
            m_workerDelegate.BeginInvoke(stream, asyncOp, m_completionMethodDelegate, null, null);
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
            Exception exception = null;

            try
            {
                dump = DoLoad(stream, asyncOp);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            LoadState loadState = new LoadState(dump, exception, asyncOp);

            try
            {
                completionMethodDelegate(loadState);
            }
            catch (InvalidOperationException)
            {
            }
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

            AsyncOperation asyncOp = loadState.m_asyncOperation;
            LoadCompletedEventArgs e = new LoadCompletedEventArgs(loadState.m_dump, loadState.m_exception, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private Dump DoLoad(Stream stream, AsyncOperation asyncOp)
        {
            Dump dump = new Dump();

            BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.ASCII);

            string magic = new string(reader.ReadChars(4));
            uint version = reader.ReadUInt32();
            uint isCompressed = reader.ReadUInt32();
            uint numEvents = reader.ReadUInt32();

            if (magic != "oSpy")
                throw new InvalidDataException("invalid signature '" + magic + "'");
            else if (version != 2)
                throw new InvalidDataException("unsupported version " + version);
            else if (isCompressed != 0 && isCompressed != 1)
                throw new InvalidDataException("invalid value for is_compressed");

            if (isCompressed == 1)
                stream = new BZip2InputStream(stream);

            XmlTextReader xmlReader = new XmlTextReader(stream);

            uint eventCount;

            for (eventCount = 0; xmlReader.Read() && eventCount < numEvents; )
            {
                if (asyncOp != null)
                {
                    int percentComplete = (int)(((float)(eventCount + 1) / (float)numEvents) * 100.0f);
                    ProgressChangedEventArgs e = new ProgressChangedEventArgs(percentComplete, asyncOp.UserSuppliedState);
                    asyncOp.Post(m_onProgressReportDelegate, e);
                }

                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "event")
                {
                    XmlReader rdr = xmlReader.ReadSubtree();
                    Event ev = m_eventFactory.CreateEvent(rdr);
                    dump.AddEvent(ev);

                    eventCount++;
                }
            }

            if (eventCount != numEvents)
                throw new InvalidDataException(String.Format("expected {0} events, read {1}", numEvents, eventCount));

            return dump;
        }

        #endregion // Core implementation
    }

    #region Helper classes

    internal class LoadState
    {
        public Dump m_dump;
        public Exception m_exception;
        public AsyncOperation m_asyncOperation;

        public LoadState(Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            m_dump = dump;
            m_exception = ex;
            m_asyncOperation = asyncOp;
        }
    }

    public class LoadCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump m_dump;

        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary();
                return m_dump;
            }
        }

        public LoadCompletedEventArgs(Dump dump, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            m_dump = dump;
        }
    }

    #endregion // Helper classes
}
