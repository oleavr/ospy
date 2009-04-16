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
using System.Text;
using System.Threading;
using System.Xml;
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpy.SharpDumpLib
{
    public delegate void SaveCompletedEventHandler(object sender, SaveCompletedEventArgs e);

    public class DumpSaver : AsyncWorker
    {
        #region Internal members

        private WorkerEventHandler m_workerDelegate;

        private delegate void WorkerEventHandler(SaveInfo info, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);

        #endregion // Internal members

        #region Events

        public event ProgressChangedEventHandler SaveProgressChanged;
        public event SaveCompletedEventHandler SaveCompleted;

        #endregion // Events

        #region Construction and destruction

        public DumpSaver()
            : base()
        {
        }

        public DumpSaver(IContainer container)
            : base(container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual void Save(Dump dump, DumpFormat format, Stream stream)
        {
            SaveInfo info = new SaveInfo(dump, format, stream);
            DoSave(info, null);
        }

        public virtual void SaveAsync(Dump dump, DumpFormat format, Stream stream, object taskId)
        {
            SaveInfo info = new SaveInfo(dump, format, stream);

            AsyncOperation asyncOp = CreateOperation(taskId);

            m_workerDelegate = new WorkerEventHandler(SaveWorker);
            m_workerDelegate.BeginInvoke(info, asyncOp, m_completionMethodDelegate, null, null);
        }

        public virtual void SaveAsyncCancel(object taskId)
        {
            CancelOperation(taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void SaveWorker(SaveInfo info, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Exception exception = null;

            try
            {
                DoSave(info, asyncOp);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            SaveState saveState = new SaveState(info, exception, asyncOp);

            try
            {
                completionMethodDelegate(saveState);
            }
            catch (InvalidOperationException)
            {
            }
        }

        protected override object CreateCancelEventArgs(object userSuppliedState)
        {
            return new SaveCompletedEventArgs(null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnSaveProgressChanged(e as ProgressChangedEventArgs);
        }

        protected virtual void OnSaveProgressChanged(ProgressChangedEventArgs e)
        {
            if (SaveProgressChanged != null)
                SaveProgressChanged(this, e);
        }

        protected override void ReportCompletion(object e)
        {
            OnSaveCompleted(e as SaveCompletedEventArgs);
        }

        protected virtual void OnSaveCompleted(SaveCompletedEventArgs e)
        {
            if (SaveCompleted != null)
                SaveCompleted(this, e);
        }

        protected override void CompletionMethod(object state)
        {
            SaveState saveState = state as SaveState;

            AsyncOperation asyncOp = saveState.m_asyncOperation;
            SaveCompletedEventArgs e = new SaveCompletedEventArgs(saveState.m_saveInfo, saveState.m_exception, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private void DoSave(SaveInfo info, AsyncOperation asyncOp)
        {
            BinaryWriter binWriter = new BinaryWriter(info.m_stream, Encoding.ASCII);

            char[] magic = "oSpy".ToCharArray();
            uint version = 2;
            uint isCompressed = (info.m_format == DumpFormat.Compressed) ? 1U : 0U;
            uint numEvents = (uint)info.m_dump.Events.Count;

            binWriter.Write(magic);
            binWriter.Write(version);
            binWriter.Write(isCompressed);
            binWriter.Write(numEvents);
            binWriter.Flush();
            binWriter = null;

            Stream stream = info.m_stream;
            if (info.m_format == DumpFormat.Compressed)
                stream = new BZip2OutputStream(stream);
            XmlTextWriter xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
            xmlWriter.WriteStartDocument(true);
            xmlWriter.WriteStartElement("events");

            int eventCount = 0;
            foreach (Event ev in info.m_dump.Events.Values)
            {
                if (asyncOp != null)
                {
                    int pctComplete = (int)(((float)(eventCount + 1) / (float)numEvents) * 100.0f);
                    ProgressChangedEventArgs e = new ProgressChangedEventArgs(pctComplete, asyncOp.UserSuppliedState);
                    asyncOp.Post(m_onProgressReportDelegate, e);
                }

                xmlWriter.WriteRaw(ev.RawData);

                eventCount++;
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
            stream.Close();
        }

        #endregion // Core implementation
    }

    public enum DumpFormat
    {
        Uncompressed,
        Compressed
    }

    #region Helper classes

    internal class SaveInfo
    {
        public Dump m_dump;
        public DumpFormat m_format;
        public Stream m_stream;

        public SaveInfo(Dump dump, DumpFormat format, Stream stream)
        {
            m_dump = dump;
            m_format = format;
            m_stream = stream;
        }
    }

    internal class SaveState
    {
        public SaveInfo m_saveInfo;
        public Exception m_exception;
        public AsyncOperation m_asyncOperation;

        public SaveState(SaveInfo info, Exception ex, AsyncOperation asyncOp)
        {
            m_saveInfo = info;

            m_exception = ex;
            m_asyncOperation = asyncOp;
        }
    }

    public class SaveCompletedEventArgs : AsyncCompletedEventArgs
    {
        private SaveInfo m_saveInfo;

        internal SaveCompletedEventArgs(SaveInfo info, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            m_saveInfo = info;
        }

        public Dump Dump
        {
            get
            {
                RaiseExceptionIfNecessary();
                return m_saveInfo.m_dump;
            }
        }

        public Stream Stream
        {
            get
            {
                RaiseExceptionIfNecessary();
                return m_saveInfo.m_stream;
            }
        }
    }

    #endregion // Helper classes
}
