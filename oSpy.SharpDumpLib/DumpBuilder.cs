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
    public delegate void BuildDumpCompletedEventHandler(object sender, BuildDumpCompletedEventArgs e);

    public class DumpBuilder : Component
    {
        #region Events
        public event ProgressChangedEventHandler ProgressChanged;
        public event BuildDumpCompletedEventHandler BuildDumpCompleted;
        #endregion // Events

        #region Internal members
        private Container components = null;

        private SendOrPostCallback onProgressReportDelegate;
        private SendOrPostCallback onCompletedDelegate;
        private SendOrPostCallback completionMethodDelegate;

        private delegate void WorkerEventHandler(string logPath, int numEvents, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        private HybridDictionary userStateToLifetime = new HybridDictionary();
        #endregion // Internal members

        #region Construction and destruction

        public DumpBuilder(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            InitializeDelegates();
        }

        public DumpBuilder()
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
            onCompletedDelegate = new SendOrPostCallback(BuildCompleted);
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

        public virtual Dump Build(string logPath, int numEvents)
        {
            return DoBuild(logPath, numEvents, null);
        }

        public virtual void BuildAsync(string logPath, int numEvents, object taskId)
        {
            AsyncOperation asyncOp = AsyncOperationManager.CreateOperation(taskId);

            lock (userStateToLifetime.SyncRoot)
            {
                if (userStateToLifetime.Contains(taskId))
                    throw new ArgumentException("Task ID parameter must be unique", "taskId");

                userStateToLifetime[taskId] = asyncOp;
            }

            workerDelegate = new WorkerEventHandler(BuildWorker);
            workerDelegate.BeginInvoke(logPath, numEvents, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void CancelAsync(object taskId)
        {
            lock (userStateToLifetime.SyncRoot)
            {
                object obj = userStateToLifetime[taskId];
                if (obj != null)
                {
                    AsyncOperation asyncOp = obj as AsyncOperation;

                    BuildDumpCompletedEventArgs e = new BuildDumpCompletedEventArgs(null, null, true, asyncOp.UserSuppliedState);
                    asyncOp.PostOperationCompleted(onCompletedDelegate, e);
                }
            }
        }

        #endregion // Public interface

        #region Core implementation

        private Dump DoBuild(string logPath, int numEvents, AsyncOperation asyncOp)
        {
            Dump dump = new Dump();

            try
            {
                uint n = 0;

                foreach (string filePath in Directory.GetFiles(logPath, "*.log", SearchOption.TopDirectoryOnly))
                {
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    BinaryReader r = new BinaryReader(fs);

                    while (fs.Position < fs.Length && n < numEvents)
                    {
                        if (asyncOp != null)
                        {
                            int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                            ProgressChangedEventArgs e = new ProgressChangedEventArgs(pctComplete, asyncOp.UserSuppliedState);
                            asyncOp.Post(onProgressReportDelegate, e);
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.AppendChild(UnserializeNode(r, doc));
                        dump.AddEvent(doc.DocumentElement);

                        n++;
                    }

                    r.Close();
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

        private XmlNode UnserializeNode(BinaryReader r, XmlDocument doc)
        {
            XmlElement el = doc.CreateElement(UnserializeString(r));

            uint attrCount = r.ReadUInt32();
            for (int i = 0; i < attrCount; i++)
            {
                XmlAttribute attr = doc.CreateAttribute(UnserializeString(r));
                attr.Value = UnserializeString(r);
                el.Attributes.Append(attr);
            }

            UInt32 contentIsRaw = r.ReadUInt32();
            string content = "";

            if (contentIsRaw != 0)
            {
                uint len = r.ReadUInt32();
                byte[] bytes = r.ReadBytes((int)len);
                if (bytes.Length > 0)
                {
                    content = Convert.ToBase64String(bytes, Base64FormattingOptions.None);
                }
            }
            else
            {
                content = UnserializeString(r);
            }

            if (content != String.Empty)
            {
                XmlText text = doc.CreateTextNode(content);
                el.AppendChild(text);
            }

            uint childCount = r.ReadUInt32();
            for (int i = 0; i < childCount; i++)
            {
                XmlNode childNode = UnserializeNode(r, doc);
                el.AppendChild(childNode);
            }

            return el;
        }

        private string UnserializeString(BinaryReader r)
        {
            uint len = r.ReadUInt32();
            byte[] buf = r.ReadBytes((int)len);

            Decoder dec = Encoding.ASCII.GetDecoder();
            char[] chars = new char[buf.Length];
            dec.GetChars(buf, 0, buf.Length, chars, 0);
            return new string(chars);
        }

        #endregion // Core implementation

        #region Async glue

        private void ReportProgress(object state)
        {
            OnProgressChanged(state as ProgressChangedEventArgs);
        }

        private void BuildCompleted(object operationState)
        {
            OnBuildCompleted(operationState as BuildDumpCompletedEventArgs);
        }

        private void CompletionMethod(object buildDumpState)
        {
            BuildDumpState buildState = buildDumpState as BuildDumpState;

            AsyncOperation asyncOp = buildState.asyncOp;
            BuildDumpCompletedEventArgs e = new BuildDumpCompletedEventArgs(buildState.dump, buildState.ex, false, asyncOp.UserSuppliedState);

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

        protected void OnBuildCompleted(BuildDumpCompletedEventArgs e)
        {
            if (BuildDumpCompleted != null)
                BuildDumpCompleted(this, e);
        }

        private void BuildWorker(string logPath, int numEvents, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            Dump dump = null;
            Exception e = null;

            try
            {
                dump = DoBuild(logPath, numEvents, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            BuildDumpState buildState = new BuildDumpState(dump, e, asyncOp);

            try { completionMethodDelegate(buildState); }
            catch (InvalidOperationException) {}
        }

        #endregion // Async glue
    }

    #region Helper classes

    internal class BuildDumpState
    {
        public Dump dump = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public BuildDumpState(Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class BuildDumpCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dump dump = null;

        public BuildDumpCompletedEventArgs(Dump dump, Exception e, bool cancelled, object state)
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

    #endregion // Helper classes
}
