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
