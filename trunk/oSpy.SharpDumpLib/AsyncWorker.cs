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
using System.Threading;
using System.Collections.Specialized;

namespace oSpy.SharpDumpLib
{
    public abstract class AsyncWorker : Component
    {
        #region Internal members

        private Container m_components = null;

        protected SendOrPostCallback m_onProgressReportDelegate;
        protected SendOrPostCallback m_onCompletedDelegate;
        protected SendOrPostCallback m_completionMethodDelegate;

        private HybridDictionary m_userStateToLifetime = new HybridDictionary();

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
            m_components = new Container();
        }

        protected void InitializeDelegates()
        {
            m_onProgressReportDelegate = new SendOrPostCallback(ReportProgress);
            m_onCompletedDelegate = new SendOrPostCallback(ReportCompletion);
            m_completionMethodDelegate = new SendOrPostCallback(CompletionMethod);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_components != null)
                    m_components.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion // Construction and destruction

        #region Internal API

        protected AsyncOperation CreateOperation(object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (m_userStateToLifetime.SyncRoot)
            {
                if (m_userStateToLifetime.Contains(taskId))
                    throw new ArgumentException("Task ID parameter must be unique", "taskId");

                m_userStateToLifetime[taskId] = asyncOp;
            }

            return asyncOp;
        }

        protected void CancelOperation(object taskId)
        {
            lock (m_userStateToLifetime.SyncRoot)
            {
                object obj = m_userStateToLifetime[taskId];
                if (obj != null)
                {
                    AsyncOperation asyncOp = obj as AsyncOperation;
                    object e = CreateCancelEventArgs(asyncOp.UserSuppliedState);
                    asyncOp.PostOperationCompleted(m_onCompletedDelegate, e);
                }
            }
        }

        protected void FinalizeOperation(AsyncOperation asyncOp, AsyncCompletedEventArgs e)
        {
            lock (m_userStateToLifetime.SyncRoot)
            {
                m_userStateToLifetime.Remove(asyncOp.UserSuppliedState);
            }

            asyncOp.PostOperationCompleted(m_onCompletedDelegate, e);
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
