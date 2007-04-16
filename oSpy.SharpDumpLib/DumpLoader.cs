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

namespace oSpy.SharpDumpLib
{
    public delegate void ProgressChangedEventHandler(ProgressChangedEventArgs e);
    public delegate void LoadDumpCompletedEventHandler(object sender, LoadDumpCompletedEventArgs e);

    public class DumpLoader : Component
    {
        public event ProgressChangedEventHandler ProgressChanged;
        public event LoadDumpCompletedEventHandler LoadDumpCompleted;

        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;
        private SendOrPostCallback completionMethodDelegate;

        private delegate void WorkerEventHandler(string path, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();

        private Container components = null;

        #region Construction and destruction

        public DumpLoader(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            InitializeDelegates();
        }

        public DumpLoader()
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
            onCompletedDelegate = new SendOrPostCallback(LoadCompleted);
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

        #region Async glue

        private void ReportProgress(object state)
        {
            ProgressChangedEventArgs e = state as ProgressChangedEventArgs;

            OnProgressChanged(e);
        }

        protected void OnProgressChanged(ProgressChangedEventArgs e)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(e);
            }
        }

        private void LoadCompleted(object operationState)
        {
            LoadDumpCompletedEventArgs e = operationState as LoadDumpCompletedEventArgs;

            OnLoadCompleted(e);
        }

        protected void OnLoadCompleted(LoadDumpCompletedEventArgs e)
        {
            if (LoadDumpCompleted != null)
            {
                LoadDumpCompleted(this, e);
            }
        }

        private void CompletionMethod(object loadDumpState)
        {
            LoadDumpState loadState = loadDumpState as LoadDumpState;

            AsyncOperation asyncOp = loadState.asyncOp;
            LoadDumpCompletedEventArgs e = new LoadDumpCompletedEventArgs(loadState.dump, loadState.ex, false, asyncOp.UserSuppliedState);

            lock (userStateToLifetime.SyncRoot)
            {
                userStateToLifetime.Remove(asyncOp.UserSuppliedState);
            }

            asyncOp.PostOperationCompleted(onCompletedDelegate, e);
        }

        private void LoadWorker(string path, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Dump dump = null;
            Exception e = null;

            try
            {
                dump = DoLoad(path, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            LoadDumpState loadState = new LoadDumpState(dump, e, asyncOp);
            completionMethodDelegate(loadState);
        }

        #endregion // Async glue

        #region Core implementation

        private Dump DoLoad(string path, AsyncOperation asyncOp)
        {
            throw new NotImplementedException("FIXME");
        }

        #endregion // Core implementation

        #region Public interface

        public Dump Load(string path)
        {
            return DoLoad(path, null);
        }

        public void LoadAsync(string path, object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                {
                    throw new ArgumentException("Task ID parameter must be unique", "taskId");
                }

                userStateToLifetime[taskId] = asyncOp;
            }

            workerDelegate = new WorkerEventHandler(LoadWorker);
            workerDelegate.BeginInvoke(path, asyncOp, completionMethodDelegate, null, null);
        }

        public void CancelAsync(object taskId)
        {
            lock (userStateToLifetime.SyncRoot)
            {
                object obj = userStateToLifetime[taskId];
                if (obj != null)
                {
                    AsyncOperation asyncOp = obj as AsyncOperation;

                    LoadDumpCompletedEventArgs e = new LoadDumpCompletedEventArgs(null, null, true, asyncOp.UserSuppliedState);
                    asyncOp.PostOperationCompleted(onCompletedDelegate, e);
                }
            }
        }

        #endregion // Public interface
    }

    internal class LoadDumpState
    {
        public Dump dump = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public LoadDumpState(Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class LoadDumpCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump = null;

        public LoadDumpCompletedEventArgs(Dump dump, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
        }

        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary();

                return dump;
            }
        }
    }
}
