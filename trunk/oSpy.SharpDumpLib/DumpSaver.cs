//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Threading;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace oSpy.SharpDumpLib
{
    public delegate void SaveDumpCompletedEventHandler(object sender, SaveDumpCompletedEventArgs e);

    public class DumpSaver : Component
    {
        #region Events
        public event ProgressChangedEventHandler ProgressChanged;
        public event SaveDumpCompletedEventHandler SaveDumpCompleted;
        #endregion // Events

        #region Internal members
        private Container components = null;

        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;
        private SendOrPostCallback completionMethodDelegate;

        private delegate void WorkerEventHandler(Dump dump, Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();
        #endregion // Internal members

        #region Construction and destruction

        public DumpSaver(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            InitializeDelegates();
        }

        public DumpSaver()
        {
            InitializeComponent();
            InitializeDelegates();
        }

        private void InitializeComponent()
        {
            components = new Container();
        }

        protected void InitializeDelegates()
        {
            onProgressReportDelegate = new SendOrPostCallback(ReportProgress);
            onCompletedDelegate = new SendOrPostCallback(SaveCompleted);
            completionMethodDelegate = new SendOrPostCallback(CompletionMethod);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual void Save(Dump dump, Stream stream)
        {
            DoSave(dump, stream, null);
        }

        public virtual void SaveAsync(Dump dump, Stream stream, object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                    throw new ArgumentException("Task ID parameter must be unique", "taskId");

                userStateToLifetime[taskId] = asyncOp;
            }

            workerDelegate = new WorkerEventHandler(SaveWorker);
            workerDelegate.BeginInvoke(dump, stream, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void CancelAsync(object taskId)
        {
            lock (userStateToLifetime.SyncRoot)
            {
                object obj = userStateToLifetime[taskId];
                if (obj != null)
                {
                    AsyncOperation asyncOp = obj as AsyncOperation;

                    SaveDumpCompletedEventArgs e = new SaveDumpCompletedEventArgs(null, null, null, true, asyncOp.UserSuppliedState);
                    asyncOp.PostOperationCompleted(onCompletedDelegate, e);
                }
            }
        }

        #endregion // Public interface

        #region Core implementation

        private void DoSave(Dump dump, Stream stream, AsyncOperation asyncOp)
        {
            BinaryWriter binWriter = new BinaryWriter(stream, Encoding.UTF8);
            binWriter.Write((uint) dump.Events.Count);
            binWriter.Flush();
            binWriter = null;

            XmlTextWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
            xmlWriter.WriteStartDocument(true);
            xmlWriter.WriteStartElement("events");

            int n = 0;
            int numEvents = dump.Events.Count;
            foreach (Event ev in dump.Events.Values)
            {
                if (asyncOp != null)
                {
                    int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                    ProgressChangedEventArgs e = new ProgressChangedEventArgs(pctComplete, asyncOp.UserSuppliedState);
                    asyncOp.Post(onProgressReportDelegate, e);
                }

                xmlWriter.WriteRaw(ev.RawData);

                n++;
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
        }

        #endregion // Core implementation

        #region Async glue

        private void ReportProgress(object state)
        {
            OnProgressChanged(state as ProgressChangedEventArgs);
        }

        private void SaveCompleted(object operationState)
        {
            OnSaveCompleted(operationState as SaveDumpCompletedEventArgs);
        }

        private void CompletionMethod(object saveDumpState)
        {
            SaveDumpState saveState = saveDumpState as SaveDumpState;

            AsyncOperation asyncOp = saveState.asyncOp;
            SaveDumpCompletedEventArgs e = new SaveDumpCompletedEventArgs(saveState.dump, saveState.stream, saveState.ex, false, asyncOp.UserSuppliedState);

            lock (userStateToLifetime.SyncRoot)
            {
                userStateToLifetime.Remove(asyncOp.UserSuppliedState);
            }

            asyncOp.PostOperationCompleted(onCompletedDelegate, e);
        }

        protected void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, e);
        }

        protected void OnSaveCompleted(SaveDumpCompletedEventArgs e)
        {
            if (SaveDumpCompleted != null)
                SaveDumpCompleted(this, e);
        }

        private void SaveWorker(Dump dump, Stream stream, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Exception e = null;

            try
            {
                DoSave(dump, stream, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            SaveDumpState saveState = new SaveDumpState(dump, stream, e, asyncOp);

            try { completionMethodDelegate(saveState); }
            catch (InvalidOperationException) { }
        }

        #endregion // Async glue
    }

    #region Helper classes

    internal class SaveDumpState
    {
        public Dump dump = null;
        public Stream stream = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public SaveDumpState(Dump dump, Stream stream, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.stream = stream;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class SaveDumpCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump = null;
        private Stream stream = null;

        public SaveDumpCompletedEventArgs(Dump dump, Stream stream, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
            this.stream = stream;
        }

        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary();

                return dump;
            }
        }

        public Stream Stream
        {
            get
            {
                RaiseExceptionIfNecessary();

                return stream;
            }
        }
    }

    #endregion // Helper classes
}
