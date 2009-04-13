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

        private Container components = null;

        protected SendOrPostCallback on_progress_report_delegate;
        protected SendOrPostCallback on_completed_delegate;
        protected SendOrPostCallback completion_method_delegate;

        private HybridDictionary user_state_to_lifetime = new HybridDictionary ();

        #endregion

        #region Construction and destruction

        public AsyncWorker (IContainer container)
        {
            container.Add (this);
            InitializeComponent ();
            InitializeDelegates ();
        }

        public AsyncWorker ()
        {
            InitializeComponent ();
            InitializeDelegates ();
        }

        private void InitializeComponent ()
        {
            components = new Container ();
        }

        protected void InitializeDelegates ()
        {
            on_progress_report_delegate = new SendOrPostCallback (ReportProgress);
            on_completed_delegate = new SendOrPostCallback (ReportCompletion);
            completion_method_delegate = new SendOrPostCallback (CompletionMethod);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (components != null)
                    components.Dispose ();
            }

            base.Dispose (disposing);
        }

        #endregion // Construction and destruction

        #region Internal API

        protected AsyncOperation CreateOperation (object taskId)
        {
            AsyncOperation async_op = AsyncOperationManager.CreateOperation (taskId);

            lock (user_state_to_lifetime.SyncRoot) {
                if (user_state_to_lifetime.Contains (taskId))
                    throw new ArgumentException ("Task ID parameter must be unique", "taskId");

                user_state_to_lifetime[taskId] = async_op;
            }

            return async_op;
        }

        protected void CancelOperation(object taskId)
        {
            lock (user_state_to_lifetime.SyncRoot) {
                object obj = user_state_to_lifetime[taskId];
                if (obj != null) {
                    AsyncOperation async_op = obj as AsyncOperation;
                    object e = CreateCancelEventArgs (async_op.UserSuppliedState);
                    async_op.PostOperationCompleted (on_completed_delegate, e);
                }
            }
        }

        protected void FinalizeOperation(AsyncOperation asyncOp, AsyncCompletedEventArgs e)
        {
            lock (user_state_to_lifetime.SyncRoot) {
                user_state_to_lifetime.Remove (asyncOp.UserSuppliedState);
            }

            asyncOp.PostOperationCompleted (on_completed_delegate, e);
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
