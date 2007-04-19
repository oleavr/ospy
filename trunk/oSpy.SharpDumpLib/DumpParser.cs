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
using System.Xml;
using System.IO;

namespace oSpy.SharpDumpLib
{
    public delegate void ParseProgressChangedEventHandler(object sender, ParseProgressChangedEventArgs e);
    public delegate void ParseCompletedEventHandler(object sender, ParseCompletedEventArgs e);

    //
    // DumpParser takes care of the low-level details of a dump, recognizing
    // related recv()/send() (for example) and grouping them into DataExchange
    // objects attached to a Resource, representing the OS handle.
    //
    // TransactionBuilder will be written later, and will take care of parsing
    // the DataExchange objects and building Transaction objects from them by
    // recognizing the protocols.
    //
    public class DumpParser : AsyncWorker
    {
        #region Events

        public event ParseProgressChangedEventHandler ParseProgressChanged;
        public event ParseCompletedEventHandler ParseCompleted;

        #endregion // Events

        #region Internal members

        private delegate void WorkerEventHandler(Dump dump, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate);
        private WorkerEventHandler workerDelegate;

        #endregion // Internal members

        #region Construction and destruction

        public DumpParser()
            : base()
        {
        }

        public DumpParser(IContainer container)
            : base(container)
        {
        }

        #endregion // Construction and destruction

        #region Public interface

        public virtual List<Resource> Parse(Dump dump)
        {
            return DoParsing(dump, null);
        }

        public virtual void ParseAsync(Dump dump, object taskId)
        {
            AsyncOperation asyncOp = CreateOperation(taskId);

            workerDelegate = new WorkerEventHandler(ParseWorker);
            workerDelegate.BeginInvoke(dump, asyncOp, completionMethodDelegate, null, null);
        }

        public virtual void ParseAsyncCancel(object taskId)
        {
            CancelOperation(taskId);
        }

        #endregion // Public interface

        #region Async glue

        private void ParseWorker(Dump dump, AsyncOperation asyncOp, SendOrPostCallback completionMethodDelegate)
        {
            List<Resource> resources = null;
            Exception e = null;

            try
            {
                resources = DoParsing(dump, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            ParseDumpState parseState = new ParseDumpState(dump, resources, e, asyncOp);

            try { completionMethodDelegate(parseState); }
            catch (InvalidOperationException) { }
        }

        protected override object CreateCancelEventArgs(object userSuppliedState)
        {
            return new ParseCompletedEventArgs(null, null, null, true, userSuppliedState);
        }

        protected override void ReportProgress(object e)
        {
            OnParseProgressChanged(e as ParseProgressChangedEventArgs);
        }

        protected virtual void OnParseProgressChanged(ParseProgressChangedEventArgs e)
        {
            if (ParseProgressChanged != null)
                ParseProgressChanged(this, e);
        }

        protected override void ReportCompletion(object e)
        {
            OnParseCompleted(e as ParseCompletedEventArgs);
        }

        protected virtual void OnParseCompleted(ParseCompletedEventArgs e)
        {
            if (ParseCompleted != null)
                ParseCompleted(this, e);
        }

        protected override void CompletionMethod(object state)
        {
            ParseDumpState parseState = state as ParseDumpState;

            AsyncOperation asyncOp = parseState.asyncOp;
            ParseCompletedEventArgs e = new ParseCompletedEventArgs(parseState.dump, parseState.resources, parseState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private List<Resource> DoParsing(Dump dump, AsyncOperation asyncOp)
        {
            List<Resource> resources = new List<Resource>();

            SortedDictionary<uint, Event> pendingEvents = new SortedDictionary<uint, Event>(dump.Events);
            List<Event> processedEvents = new List<Event>();
            int n = 0;
            int numEvents = dump.Events.Count;

            while (pendingEvents.Count > 0)
            {
                Resource curRes = null;

                foreach (Event ev in pendingEvents.Values)
                {
                    bool processed = false;

                    if (ev.Type == DumpEventType.FunctionCall)
                    {
                        XmlElement eventRoot = ev.Data;

                        ResourceType resType;
                        UInt32 handle;
                        FunctionCallType callType = InspectFunctionCallEvent(eventRoot, out resType, out handle);

                        switch (callType)
                        {
                            case FunctionCallType.SocketCreate:
                            case FunctionCallType.SocketRecv:
                            case FunctionCallType.SocketSend:
                                if (curRes == null)
                                {
                                    processed = true;

                                    if (resType == ResourceType.Socket)
                                    {
                                        if (callType == FunctionCallType.SocketCreate)
                                        {
                                            curRes = ParseSocketCreateEvent(eventRoot, handle);
                                        }
                                        else
                                        {
                                            curRes = new SocketResource(handle);
                                        }
                                    }
                                }

                                break;
                            case FunctionCallType.SocketClose:
                                if (curRes != null && handle == curRes.Handle)
                                {
                                    resources.Add(curRes);
                                    if (asyncOp != null)
                                    {
                                        int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                                        ParseProgressChangedEventArgs e = new ParseProgressChangedEventArgs(curRes, pctComplete, asyncOp.UserSuppliedState);
                                        asyncOp.Post(onProgressReportDelegate, e);
                                    }
                                    curRes = null;

                                    processed = true;
                                }

                                break;
                            case FunctionCallType.Unknown:
                            default:
                                processed = true;
                                break;
                        }

                        if (curRes != null && handle == curRes.Handle)
                        {
                            switch (callType)
                            {
                                case FunctionCallType.SocketRecv:
                                case FunctionCallType.SocketSend:
                                    byte[] buf = ParseSocketRecvSendEvent(eventRoot);
                                    if (buf != null)
                                    {
                                        DataDirection direction =
                                            (callType == FunctionCallType.SocketRecv) ? DataDirection.Incoming : DataDirection.Outgoing;

                                        curRes.AppendData(buf, direction);
                                    }
                                    processed = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        processed = true;
                    }

                    if (processed)
                    {
                        processedEvents.Add(ev);
                        n++;

                        if (asyncOp != null)
                        {
                            int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                            ParseProgressChangedEventArgs e = new ParseProgressChangedEventArgs(curRes, pctComplete, asyncOp.UserSuppliedState);
                            asyncOp.Post(onProgressReportDelegate, e);
                        }
                    }
                }

                if (processedEvents.Count == 0)
                    throw new InvalidDataException(String.Format("{0} pending events could not be processed", pendingEvents.Count));

                foreach (Event ev in processedEvents)
                {
                    pendingEvents.Remove(ev.Id);
                }
                processedEvents.Clear();
            }

            return resources;
        }

        private const string firstArgInValQuery = "/event/arguments[@direction='in']/argument[1]/value/@value";
        private const string secondArgInValQuery = "/event/arguments[@direction='in']/argument[2]/value/@value";
        private const string retValQuery = "/event/returnValue/value/@value";

        private FunctionCallType InspectFunctionCallEvent(XmlElement eventRoot, out ResourceType type, out UInt32 handle)
        {
            type = ResourceType.Unknown;
            handle = 0;
            FunctionCallType callType = FunctionCallType.Unknown;
            string query = null;

            XmlNode node = eventRoot.SelectSingleNode("/event/name");
            if (node == null)
                throw new InvalidDataException("name element not found on FunctionCall event");
            string functionName = node.InnerText.ToLower();

            if (functionName.StartsWith("ws2_32.dll::"))
            {
                type = ResourceType.Socket;

                functionName = functionName.Substring(12);

                if (functionName == "recv")
                {
                    callType = FunctionCallType.SocketRecv;
                    query = firstArgInValQuery;
                }
                else if (functionName == "send")
                {
                    callType = FunctionCallType.SocketSend;
                    query = firstArgInValQuery;
                }
                else if (functionName == "socket")
                {
                    callType = FunctionCallType.SocketCreate;
                    query = retValQuery;
                }
                else if (functionName == "closesocket")
                {
                    callType = FunctionCallType.SocketClose;
                    query = firstArgInValQuery;
                }
            }

            if (query != null)
            {
                node = eventRoot.SelectSingleNode(query);
                if (node == null)
                    throw new InvalidDataException("value element not found");
                handle = ParseUIntNumber(node.Value);
            }

            return callType;
        }

        private SocketResource ParseSocketCreateEvent(XmlElement eventRoot, UInt32 handle)
        {
            XmlNode node = eventRoot.SelectSingleNode(firstArgInValQuery);
            if (node == null)
                throw new InvalidDataException("first argument to socket() not found");
            AddressFamily addrFamily = (AddressFamily) Enum.Parse(typeof(AddressFamily), node.Value);

            node = eventRoot.SelectSingleNode(secondArgInValQuery);
            if (node == null)
                throw new InvalidDataException("second argument to socket() not found");
            SocketType sockType = (SocketType)Enum.Parse(typeof(SocketType), node.Value);

            return new SocketResource(handle, addrFamily, sockType);
        }

        private byte[] ParseSocketRecvSendEvent(XmlElement eventRoot)
        {
            XmlNode node = eventRoot.SelectSingleNode("/event/arguments/argument/value[@type='Pointer']/value[@type='ByteArray']");
            if (node == null)
                return null;
            byte[] buf = Convert.FromBase64String(node.InnerText);

            node = eventRoot.SelectSingleNode(retValQuery);
            if (node == null)
                throw new InvalidDataException("ReturnValue element not found");
            UInt32 retVal = ParseUIntNumber(node.Value);

            if (retVal == buf.Length)
                return buf;
            else
            {
                byte[] bufSubset = new byte[retVal];
                Array.Copy(buf, bufSubset, retVal);
                return bufSubset;
            }
        }

        private UInt32 ParseUIntNumber(string s)
        {
            if (s.StartsWith("0x"))
                return UInt32.Parse(s.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
            else
                return UInt32.Parse(s);
        }

        #endregion // Core implementation
    }

    #region Internal enums

    internal enum FunctionCallType
    {
        Unknown,
        SocketCreate,
        SocketClose,
        SocketRecv,
        SocketSend,
    }

    #endregion // Internal enums

    #region Helper classes

    internal class ParseDumpState
    {
        public Dump dump = null;
        public List<Resource> resources = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public ParseDumpState(Dump dump, List<Resource> resources, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.resources = resources;
            this.ex = ex;
            this.asyncOp = asyncOp;
        }
    }

    public class ParseProgressChangedEventArgs : ProgressChangedEventArgs
    {
        private Resource latestResource = null;
        public Resource LatestResource
        {
            get { return latestResource; }
        }

        public ParseProgressChangedEventArgs(Resource latestResource, int progressPercentage, object userToken)
            : base(progressPercentage, userToken)
        {
            this.latestResource = latestResource;
        }
    }

    public class ParseCompletedEventArgs : AsyncCompletedEventArgs
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

        public ParseCompletedEventArgs(Dump dump, List<Resource> resources, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
            this.resources = resources;
        }
    }

    #endregion // Helper classes
}
