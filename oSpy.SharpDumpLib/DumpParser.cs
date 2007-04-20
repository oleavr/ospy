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
using System.Net;

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

        public virtual List<Process> Parse(Dump dump)
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
            List<Process> processes = null;
            Exception e = null;

            try
            {
                processes = DoParsing(dump, asyncOp);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            ParseDumpState parseState = new ParseDumpState(dump, processes, e, asyncOp);

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
            ParseCompletedEventArgs e = new ParseCompletedEventArgs(parseState.dump, parseState.processes, parseState.ex, false, asyncOp.UserSuppliedState);
            FinalizeOperation(asyncOp, e);
        }

        #endregion // Async glue

        #region Core implementation

        private List<Process> DoParsing(Dump dump, AsyncOperation asyncOp)
        {
            SortedDictionary<uint, Process> processes = new SortedDictionary<uint, Process>();

            try
            {
                ProcessDumpEvents(dump, asyncOp, processes);
            }
            catch (Exception e)
            {
                foreach (Process proc in processes.Values)
                {
                    proc.Close();
                }
                processes.Clear();

                throw e;
            }

            return new List<Process>(processes.Values);
        }

        private void ProcessDumpEvents(Dump dump, AsyncOperation asyncOp, SortedDictionary<uint, Process> processes)
        {
            SortedDictionary<uint, Event> pendingEvents = new SortedDictionary<uint, Event>(dump.Events);
            List<Event> processedEvents = new List<Event>();
            int n = 0;
            int numEvents = dump.Events.Count;

            while (pendingEvents.Count > 0)
            {
                Process curProcess = null;
                Resource curRes = null;
                bool doneWithCurrent = false;

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
                            case FunctionCallType.SocketConnect:
                            case FunctionCallType.SocketRecv:
                            case FunctionCallType.SocketSend:
                                if (curRes == null)
                                {
                                    processed = true;

                                    if (!processes.ContainsKey(ev.ProcessId))
                                    {
                                        processes[ev.ProcessId] = new Process(ev.ProcessId, ev.ProcessName);
                                    }
                                    curProcess = processes[ev.ProcessId];

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
                                    // TODO: add the rest of the cases here

                                    curProcess.Resources.Add(curRes);
                                }

                                break;
                            case FunctionCallType.SocketClose:
                                if (curProcess != null && handle == curRes.Handle && ev.ProcessId == curProcess.Id)
                                {
                                    if (asyncOp != null)
                                    {
                                        int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                                        ParseProgressChangedEventArgs e = new ParseProgressChangedEventArgs(curRes, pctComplete, asyncOp.UserSuppliedState);
                                        asyncOp.Post(onProgressReportDelegate, e);
                                    }
                                    curProcess = null;
                                    curRes = null;
                                    doneWithCurrent = true;

                                    processed = true;
                                }

                                break;
                            case FunctionCallType.Unknown:
                            default:
                                processed = true;
                                break;
                        }

                        if (curProcess != null && handle == curRes.Handle && ev.ProcessId == curProcess.Id)
                        {
                            switch (callType)
                            {
                                case FunctionCallType.SocketConnect:
                                    (curRes as SocketResource).SetCurrentRemoteEndpoint(ParseSocketConnectEvent(eventRoot));
                                    break;
                                case FunctionCallType.SocketRecv:
                                case FunctionCallType.SocketSend:
                                    byte[] buf = ParseSocketRecvOrSendEvent(eventRoot);
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

                    if (doneWithCurrent)
                        break;
                }

                if (processedEvents.Count == 0)
                    throw new InvalidDataException(String.Format("{0} pending events could not be processed", pendingEvents.Count));

                foreach (Event ev in processedEvents)
                {
                    pendingEvents.Remove(ev.Id);
                }
                processedEvents.Clear();

                if (curRes != null)
                {
                    if (asyncOp != null)
                    {
                        int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                        ParseProgressChangedEventArgs e = new ParseProgressChangedEventArgs(curRes, pctComplete, asyncOp.UserSuppliedState);
                        asyncOp.Post(onProgressReportDelegate, e);
                    }
                }
            }
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
                else if (functionName == "connect")
                {
                    callType = FunctionCallType.SocketConnect;
                    query = firstArgInValQuery;
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
                handle = ParseUInt32Number(node.Value);
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

        private IPEndPoint ParseSocketConnectEvent(XmlElement eventRoot)
        {
            XmlNode structNode = eventRoot.SelectSingleNode("/event/arguments[@direction='in']/argument[2]/value/value");
            if (structNode == null)
                throw new InvalidDataException("second argument to connect() not found");

            XmlNode node = structNode.SelectSingleNode("field[@name='sin_addr']/value/@value");
            string addr = node.InnerText;

            node = structNode.SelectSingleNode("field[@name='sin_port']/value/@value");
            UInt16 port = ParseUInt16Number(node.InnerText);

            return new IPEndPoint(IPAddress.Parse(addr), port);
        }

        private byte[] ParseSocketRecvOrSendEvent(XmlElement eventRoot)
        {
            XmlNode node = eventRoot.SelectSingleNode("/event/arguments/argument/value[@type='Pointer']/value[@type='ByteArray']");
            if (node == null)
                return null;
            byte[] buf = Convert.FromBase64String(node.InnerText);

            node = eventRoot.SelectSingleNode(retValQuery);
            if (node == null)
                throw new InvalidDataException("ReturnValue element not found");
            UInt32 retVal = ParseUInt32Number(node.Value);

            if (retVal == buf.Length)
                return buf;
            else
            {
                byte[] bufSubset = new byte[retVal];
                Array.Copy(buf, bufSubset, retVal);
                return bufSubset;
            }
        }

        private UInt16 ParseUInt16Number(string s)
        {
            if (s.StartsWith("0x"))
                return UInt16.Parse(s.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
            else
                return UInt16.Parse(s);
        }

        private UInt32 ParseUInt32Number(string s)
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
        SocketConnect,
        SocketClose,
        SocketRecv,
        SocketSend,
    }

    #endregion // Internal enums

    #region Helper classes

    internal class ParseDumpState
    {
        public Dump dump = null;
        public List<Process> processes = null;
        public Exception ex = null;
        public AsyncOperation asyncOp = null;

        public ParseDumpState(Dump dump, List<Process> processes, Exception ex, AsyncOperation asyncOp)
        {
            this.dump = dump;
            this.processes = processes;
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

        private List<Process> processes = null;
        public List<Process> Processes
        {
            get
            {
                RaiseExceptionIfNecessary();
                return processes;
            }
        }

        public ParseCompletedEventArgs(Dump dump, List<Process> processes, Exception e, bool cancelled, object state)
            : base(e, cancelled, state)
        {
            this.dump = dump;
            this.processes = processes;
        }
    }

    #endregion // Helper classes
}
