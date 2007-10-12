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
    // related recv()/send() (for example) and grouping them into DataTransfer
    // objects attached to a Resource, representing the OS handle.
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

        // TODO: quick 'n dirty naive implementation, lots of room for optimizations
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

                        string functionName;
                        ResourceType resType;
                        UInt32 handle;
                        FunctionCallType callType = InspectFunctionCallEvent(eventRoot, out functionName, out resType, out handle);

                        switch (callType)
                        {
                            case FunctionCallType.SocketCreate:
                            case FunctionCallType.SocketConnect:
                            case FunctionCallType.SocketRecv:
                            case FunctionCallType.SocketSend:
                            // TODO: Add recvfrom() and sendto() handling
                            case FunctionCallType.EncryptMessage:
                            case FunctionCallType.DecryptMessage:
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
                                    else if (resType == ResourceType.Crypto)
                                    {
                                        curRes = new CryptoResource(handle);
                                    }

                                    curProcess.Resources.Add(curRes);
                                }

                                break;
                            case FunctionCallType.SocketClose:
                                if (curProcess != null && handle == curRes.Handle && ev.ProcessId == curProcess.Id)
                                {
                                    processed = true;

                                    if (asyncOp != null)
                                    {
                                        int pctComplete = (int)(((float)n / (float)numEvents) * 100.0f);
                                        ParseProgressChangedEventArgs e = new ParseProgressChangedEventArgs(curRes, pctComplete, asyncOp.UserSuppliedState);
                                        asyncOp.Post(onProgressReportDelegate, e);
                                    }
                                    curProcess = null;
                                    curRes = null;
                                    doneWithCurrent = true;
                                }

                                break;
                            case FunctionCallType.Unknown:
                            default:
                                processed = true;
                                break;
                        }

                        if (curProcess != null && handle == curRes.Handle && ev.ProcessId == curProcess.Id)
                        {
                            byte[] buf;
                            DataDirection direction;

                            switch (callType)
                            {
                                case FunctionCallType.SocketConnect:
                                    processed = true;
                                    (curRes as SocketResource).SetCurrentRemoteEndpoint(ParseSocketConnectEvent(eventRoot));
                                    break;
                                case FunctionCallType.SocketRecv:
                                case FunctionCallType.SocketSend:
                                    processed = true;

                                    buf = ParseSocketRecvOrSendEvent (eventRoot);
                                    if (buf != null)
                                    {
                                        direction = (callType == FunctionCallType.SocketRecv) ? DataDirection.Incoming : DataDirection.Outgoing;

                                        curRes.AppendData (buf, direction, ev.Id, functionName);
                                    }
                                    break;
                                case FunctionCallType.EncryptMessage:
                                case FunctionCallType.DecryptMessage:
                                    processed = true;

                                    buf = ParseEncryptOrDecryptMessageEvent(eventRoot, (callType == FunctionCallType.EncryptMessage));
                                    if (buf.Length > 0)
                                    {
                                        direction = (callType == FunctionCallType.DecryptMessage) ? DataDirection.Incoming : DataDirection.Outgoing;

                                        curRes.AppendData(buf, direction, ev.Id, functionName);
                                    }
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

        private FunctionCallType InspectFunctionCallEvent(XmlElement eventRoot, out string functionName, out ResourceType type, out UInt32 handle)
        {
            type = ResourceType.Unknown;
            handle = 0;
            FunctionCallType callType = FunctionCallType.Unknown;
            string query = null;

            XmlNode node = eventRoot.SelectSingleNode("/event/name");
            if (node == null)
                throw new InvalidDataException("name element not found on FunctionCall event");
            functionName = node.InnerText;

            string[] parts = functionName.ToLower ().Split (new string[] { "::" }, 2, StringSplitOptions.None);
            string module = parts[0];
            string fn = parts[1];
            if (module == "ws2_32.dll" || module == "wsock32.dll")
            {
                type = ResourceType.Socket;

                if (fn == "recv")
                {
                    callType = FunctionCallType.SocketRecv;
                    query = firstArgInValQuery;
                }
                else if (fn == "send")
                {
                    callType = FunctionCallType.SocketSend;
                    query = firstArgInValQuery;
                }
                else if (fn == "socket")
                {
                    callType = FunctionCallType.SocketCreate;
                    query = retValQuery;
                }
                else if (fn == "connect")
                {
                    callType = FunctionCallType.SocketConnect;
                    query = firstArgInValQuery;
                }
                else if (fn == "closesocket")
                {
                    callType = FunctionCallType.SocketClose;
                    query = firstArgInValQuery;
                }
            }
            else if (module == "secur32.dll")
            {
                type = ResourceType.Crypto;

                if (fn == "encryptmessage")
                {
                    callType = FunctionCallType.EncryptMessage;
                    query = firstArgInValQuery;
                }
                else if (fn == "decryptmessage")
                {
                    callType = FunctionCallType.DecryptMessage;
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

        private byte[] ParseEncryptOrDecryptMessageEvent(XmlElement eventRoot, bool isEncryptEvent)
        {
            MemoryStream stream = new MemoryStream(2048);

            string query = String.Format("((/event/arguments[@direction='{0}']/argument/value/value/field/value/value/value)[field/value/@value='SECBUFFER_DATA'])/field/value/value",
                (isEncryptEvent) ? "in" : "out");
            XmlNodeList nodes = eventRoot.SelectNodes(query);
            foreach (XmlNode node in nodes)
            {
                uint size = ParseUInt32Number(node.Attributes["size"].Value);

                byte[] buf = Convert.FromBase64String(node.InnerText);
                if (buf.Length != size)
                    throw new InvalidDataException("Failed to base64-decode data in EncryptMessage event");
                stream.Write(buf, 0, buf.Length);
            }

            return stream.ToArray();
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
        EncryptMessage,
        DecryptMessage,
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
