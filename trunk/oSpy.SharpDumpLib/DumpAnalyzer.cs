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
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;

namespace oSpy.SharpDumpLib
{
    public delegate void AnalyzeDumpProgressChangedEventHandler(object sender, AnalyzeDumpProgressChangedEventArgs e);
    public delegate void AnalyzeDumpCompletedEventHandler(object sender, AnalyzeDumpCompletedEventArgs e);

    public class DumpAnalyzer : AsyncWorker
    {
        #region Events

        public event AnalyzeDumpProgressChangedEventHandler AnalyzeProgressChanged;
        public event AnalyzeDumpCompletedEventHandler AnalyzeCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler(Dump dump, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpAnalyzer(IContainer container)
            : base(container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual List<Resource> Analyze(Dump dump)
        {
            return DoAnalysis(dump, null);
        }

        public virtual void AnalyzeAsync(Dump dump, object taskId)
        {
            AsyncOperation asyncOp = CreateOperation(taskId);

            workerDelegate = new WorkerEventHandler(AnalyzeWorker);
            workerDelegate.BeginInvoke(dump, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void AnalyzeAsyncCancel(object taskId)
        {
            CancelOperation(taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void AnalyzeWorker(Dump dump, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            List<Resource> resources = null;
            Exception e = null;

            try
            {
                resources = DoAnalysis(dump, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            AnalyzeDumpState analyzeState = new AnalyzeDumpState(dump, resources, e, asyncOp);

            try { completionMethodDelegate(analyzeState); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs(object userSuppliedState)
        {
            return new AnalyzeDumpCompletedEventArgs(null, null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnAnalyzeProgressChanged(e as AnalyzeDumpProgressChangedEventArgs);
        }

        protected virtual void OnAnalyzeProgressChanged(AnalyzeDumpProgressChangedEventArgs e)
        {
            if (AnalyzeProgressChanged != null)
                AnalyzeProgressChanged(this, e);
        }

        protected override void ReportCompletion(object e)
        {
            OnAnalyzeCompleted(e as AnalyzeDumpCompletedEventArgs);
        }

        protected virtual void OnAnalyzeCompleted(AnalyzeDumpCompletedEventArgs e)
        {
            if (AnalyzeCompleted != null)
                AnalyzeCompleted(this, e);
        }

        protected override void CompletionMethod(object state)
        {
            AnalyzeDumpState analyzeState = state as AnalyzeDumpState;

            AsyncOperation asyncOp = analyzeState.asyncOp;
            AnalyzeDumpCompletedEventArgs e = new AnalyzeDumpCompletedEventArgs(analyzeState.dump, analyzeState.resources, analyzeState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private List<Resource> DoAnalysis(Dump dump, AsyncOperation asyncOp)
        {
            return null;
        }

        #endregion // Core implementation
    }

    #region Helper classes

    internal class AnalyzeDumpState
    {
        public Dump dump = null;
        public List<Resource> resources = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public AnalyzeDumpState(Dump dump, List<Resource> resources, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.resources = resources;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class AnalyzeDumpProgressChangedEventArgs : ProgressChangedEventArgs
    {
        private Resource latestResource = null;
        public Resource LatestResource
        {
            get { return latestResource; }
        }

        public AnalyzeDumpProgressChangedEventArgs(Resource latestResource, int progressPercentage, object userToken)
            : base(progressPercentage, userToken)
        {
            this.latestResource = latestResource;
        }
    }

    public class AnalyzeDumpCompletedEventArgs : AsyncCompletedEventArgs
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

        private List<Resource> resources = null;
        public List<Resource> Resources
        {
            get
            {
                RaiseExceptionIfNecessary();
                return resources;
            }
        }

        public AnalyzeDumpCompletedEventArgs(Dump dump, List<Resource> resources, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
            this.resources = resources;
        }
    }

    #endregion // Helper classes
}
