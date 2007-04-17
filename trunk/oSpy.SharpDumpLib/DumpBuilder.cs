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
using System.IO;
using System.Text;
using System.Xml;

namespace oSpy.SharpDumpLib
{
    public delegate void BuildCompletedEventHandler(object sender, BuildCompletedEventArgs e);

    public class DumpBuilder : AsyncWorker
    {
        #region Events

        public event ProgressChangedEventHandler BuildProgressChanged;
        public event BuildCompletedEventHandler BuildCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler(string logPath, int numEvents, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpBuilder()
            : base()
        {
        }

        public DumpBuilder(IContainer container)
            : base(container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual Dump Build(string logPath, int numEvents)
        {
            return DoBuild(logPath, numEvents, null);
        }

        public virtual void BuildAsync(string logPath, int numEvents, object taskId)
        {
            AsyncOperation asyncOp = CreateOperation(taskId);

            workerDelegate = new WorkerEventHandler(BuildWorker);
            workerDelegate.BeginInvoke(logPath, numEvents, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void BuildAsyncCancel(object taskId)
        {
            CancelOperation(taskId);
        }

        #endregion // Public interface

        #region Async glue

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

            BuildState buildState = new BuildState(dump, e, asyncOp);

            try { completionMethodDelegate(buildState); }
            catch (InvalidOperationException) {}
        }

        protected override object CreateCancelEventArgs(object userSuppliedState)
        {
            return new BuildCompletedEventArgs(null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnBuildProgressChanged(e as ProgressChangedEventArgs);
        }

        protected virtual void OnBuildProgressChanged(ProgressChangedEventArgs e)
        {
            if (BuildProgressChanged != null)
                BuildProgressChanged(this, e);
        }

        protected override void ReportCompletion(object e)
        {
            OnBuildCompleted(e as BuildCompletedEventArgs);
        }

        protected virtual void OnBuildCompleted(BuildCompletedEventArgs e)
        {
            if (BuildCompleted != null)
                BuildCompleted(this, e);
        }

        protected override void CompletionMethod(object state)
        {
            BuildState buildState = state as BuildState;

            AsyncOperation asyncOp = buildState.asyncOp;
            BuildCompletedEventArgs e = new BuildCompletedEventArgs(buildState.dump, buildState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

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
                            int pctComplete = (int)(((float)(n + 1) / (float)numEvents) * 100.0f);
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
    }

    #region Helper classes

    internal class BuildState
    {
        public Dump dump = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public BuildState(Dump dump, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class BuildCompletedEventArgs : AsyncCompletedEventArgs
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

        public BuildCompletedEventArgs(Dump dump, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
        }
    }

    #endregion // Helper classes
}
