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
using System.Collections.Specialized;

namespace oSpy.SharpDumpLib
{
    public abstract class AsyncWorker : Component
    {
        #region Internal members
        
        private Container components = null;

        protected SendOrPostCallback onProgressReportDelegate;
        protected SendOrPostCallback onCompletedDelegate;
        protected SendOrPostCallback completionMethodDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();

        #endregion

        #region Construction and destruction

        public AsyncWorker(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            InitializeDelegates();
        }

        public AsyncWorker()
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
            onCompletedDelegate = new SendOrPostCallback(ReportCompletion);
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

        #region Internal API

        protected AsyncOperation CreateOperation(object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                    throw new ArgumentException("Task ID parameter must be unique", "taskId");

                userStateToLifetime[taskId] = asyncOp;
            }

            return asyncOp;
        }

        protected void CancelOperation(object taskId)
        {
            lock (userStateToLifetime.SyncRoot)
            {
                object obj = userStateToLifetime[taskId];
                if (obj != null)
                {
                    AsyncOperation asyncOp = obj as AsyncOperation;
                    object e = CreateCancelEventArgs(asyncOp.UserSuppliedState);
                    asyncOp.PostOperationCompleted(onCompletedDelegate, e);
                }
            }
        }

        protected void FinalizeOperation(AsyncOperation asyncOp, AsyncCompletedEventArgs e)
        {
            lock (userStateToLifetime.SyncRoot)
            {
                userStateToLifetime.Remove(asyncOp.UserSuppliedState);
            }

            asyncOp.PostOperationCompleted(onCompletedDelegate, e);
        }

        #endregion // Internal API

        #region Pure methods

        protected abstract object CreateCancelEventArgs(object userSuppliedState);
        protected abstract void ReportProgress(object e);
        protected abstract void ReportCompletion(object e);
        protected abstract void CompletionMethod(object state);

        #endregion // Pure methods
    }
}
