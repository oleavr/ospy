//
// Copyright (c) 2007-2009 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Linq;
using System.Threading;
using Frida;
using Frida.Events;

namespace oSpy.Capture
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public class SoftwallRule
    {
        /* mask of conditions */
        public Int32 conditions;

        /* condition values */
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string process_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string function_name;
        public UInt32 return_address;
        public UInt32 local_address;
        public UInt32 local_port;
        public UInt32 remote_address;
        public UInt32 remote_port;

        /* return value and lasterror to set if all conditions match */
        public Int32 retval;
        public UInt32 last_error;
    };

    public class Details
    {
        private SoftwallRule[] softwallRules;

        public SoftwallRule[] SoftwallRules
        {
            get
            {
                return softwallRules;
            }

            set
            {
                softwallRules = value;
            }
        }
    }

    public class CreateDetails : Details
    {
        private ProcessStartInfo info;

        public ProcessStartInfo Info
        {
            get
            {
                return info;
            }
        }

        public CreateDetails(ProcessStartInfo info)
        {
            this.info = info;
        }
    }

    public class AttachDetails : Details
    {
        private Process[] processes;

        public Process[] Processes
        {
            get
            {
                return processes;
            }
        }

        public int[] ProcessIds
        {
            get
            {
                List<int> result = new List<int>();
                foreach (Process proc in processes)
                    result.Add(proc.Id);
                return result.ToArray();
            }
        }

        public AttachDetails(Process[] processes)
        {
            this.processes = processes;
        }
    }

    public class Manager : IDisposable
    {
        public class EventStats
        {
            internal uint msgCount;
            internal uint msgBytes;
            internal uint pktCount;
            internal uint pktBytes;

            public uint MessageCount
            {
                get
                {
                    return msgCount;
                }
            }

            public uint MessageBytes
            {
                get
                {
                    return msgBytes;
                }
            }

            public uint PacketCount
            {
                get
                {
                    return pktCount;
                }
            }

            public uint PacketBytes
            {
                get
                {
                    return pktBytes;
                }
            }
        }

        public event EventHandler EventStatsChanged;

        public EventStats Stats
        {
            get
            {
                if (eventStats == null)
                    throw new InvalidOperationException();
                return eventStats;
            }
        }

        public const int SOFTWALL_CONDITION_PROCESS_NAME = 1;
        public const int SOFTWALL_CONDITION_FUNCTION_NAME = 2;
        public const int SOFTWALL_CONDITION_RETURN_ADDRESS = 4;
        public const int SOFTWALL_CONDITION_LOCAL_ADDRESS = 8;
        public const int SOFTWALL_CONDITION_LOCAL_PORT = 16;
        public const int SOFTWALL_CONDITION_REMOTE_ADDRESS = 32;
        public const int SOFTWALL_CONDITION_REMOTE_PORT = 64;

        /* connect() errors */
        public const int WSAEHOSTUNREACH = 10065;

        private readonly FridaDeviceManager deviceManager;
        private readonly FridaDevice localDevice;
        private readonly EventCoordinator eventCoordinator;
        private readonly List<FridaSession> sessions;
        private readonly List<FridaScript> scripts;

        private Details details;
        private IProgressFeedback progress;
        private Thread startWorkerThread;
        private Thread stopWorkerThread;
        private EventStats eventStats;
        private List<Event> eventQueue;
        private ManualResetEvent stopRequest;

        private static readonly string agentSource = LoadAgentSource();

        public Manager()
        {
            deviceManager = new FridaDeviceManager();
            localDevice = deviceManager.FindDeviceByType(DeviceType.Local, 5000).GetAwaiter().GetResult();
            eventCoordinator = new EventCoordinator();
            sessions = new List<FridaSession>();
            scripts = new List<FridaScript>();
            stopRequest = new ManualResetEvent(false);
        }

        public void StartCapture(Details details, IProgressFeedback progress)
        {
            if (startWorkerThread != null || stopWorkerThread != null)
                throw new InvalidOperationException();

            this.details = details;
            this.progress = progress;

            stopRequest.Reset();

            startWorkerThread = new Thread(DoStartCapture);
            startWorkerThread.Start();
        }

        public void StopCapture(IProgressFeedback progress)
        {
            if (stopWorkerThread != null)
                throw new InvalidOperationException();

            stopRequest.Set();

            while (startWorkerThread != null)
                Thread.Sleep(20);

            this.progress = progress;

            stopWorkerThread = new Thread(DoStopCapture);
            stopWorkerThread.Start();
        }

        public Event[] HarvestEvents()
        {
            if (eventQueue == null)
                throw new InvalidOperationException();
            Event[] result = eventQueue.ToArray();
            eventQueue = null;
            return result;
        }

        public void Dispose()
        {
            deviceManager.Dispose();
        }

        private void DoStartCapture()
        {
            try
            {
                PrepareCapture();

                if (details is CreateDetails)
                    CaptureCreatedProcess((CreateDetails) details);
                else
                    CaptureExistingProcesses((AttachDetails) details);

                progress.OperationComplete();
            }
            catch (Exception e)
            {
                progress.OperationFailed(e.Message);
                TearDownCapture(false);
            }

            progress = null;
            startWorkerThread = null;
        }

        private void DoStopCapture()
        {
            progress.ProgressUpdate("Stopping capture", 100);

            TearDownCapture(true);

            stopRequest.Reset();

            progress.OperationComplete();
            progress = null;
            stopWorkerThread = null;
        }

        private void PrepareCapture()
        {
            eventQueue = new List<Event>();
            eventStats = new EventStats();
        }

        private void CaptureCreatedProcess(CreateDetails createDetails)
        {
            progress.ProgressUpdate("Creating process and injecting logging agent", 100);

            ProcessStartInfo info = createDetails.Info;

            Frida.Data.SpawnOptions options = new Frida.Data.SpawnOptions();
            if (info.Arguments != String.Empty)
                options.Argv = BuildArgv(info.FileName, info.Arguments);
            if (info.WorkingDirectory != String.Empty)
                options.Cwd = info.WorkingDirectory;

            uint pid = localDevice.Spawn(info.FileName, options).GetAwaiter().GetResult();
            InstrumentProcess(pid);
            localDevice.Resume(pid).GetAwaiter().GetResult();
        }

        private void CaptureExistingProcesses(AttachDetails attachDetails)
        {
            Process[] processes = attachDetails.Processes;
            for (int i = 0; i < processes.Length; i++)
            {
                int percentComplete = (int) (((float) (i + 1) / (float) processes.Length) * 100.0f);
                progress.ProgressUpdate("Injecting logging agent" + ((processes.Length == 1) ? "" : "s"), percentComplete);
                InstrumentProcess((uint) processes[i].Id);
            }
        }

        private void InstrumentProcess(uint pid)
        {
            string processName = Process.GetProcessById((int) pid).ProcessName;

            FridaSession session = localDevice.Attach(pid).GetAwaiter().GetResult();
            sessions.Add(session);

            FridaScript script = session.CreateScript(agentSource).GetAwaiter().GetResult();
            script.OnMessage += (sender, e) => OnScriptMessage(pid, processName, e);
            script.Load().GetAwaiter().GetResult();
            ConfigureSoftwall(script);
            scripts.Add(script);
        }

        private void ConfigureSoftwall(FridaScript script)
        {
            SoftwallRule[] rules = details.SoftwallRules;
            if (rules == null || rules.Length == 0)
                return;

            var mappedRules = rules.Select(rule => new
            {
                conditions = rule.conditions,
                processName = rule.process_name,
                functionName = rule.function_name,
                returnAddress = rule.return_address,
                localAddress = rule.local_address,
                localPort = rule.local_port,
                remoteAddress = rule.remote_address,
                remotePort = rule.remote_port,
                retval = rule.retval,
                lastError = rule.last_error,
            }).ToArray();

            script.Exports.Configure(mappedRules).GetAwaiter().GetResult();
        }

        private void TearDownCapture(bool captureStarted)
        {
            foreach (FridaScript script in scripts)
            {
                if (captureStarted)
                    script.Exports.Flush().GetAwaiter().GetResult();
                script.Dispose();
            }
            foreach (FridaSession session in sessions)
                session.Dispose();
            scripts.Clear();
            sessions.Clear();

            if (captureStarted)
                eventQueue.Sort();
            else
                eventQueue = null;
            eventStats = null;

            details = null;
        }

        private void OnScriptMessage(uint pid, string processName, ScriptMessageEventArgs e)
        {
            using JsonDocument document = JsonDocument.Parse(e.Json);
            JsonElement root = document.RootElement;
            if (root.GetProperty("type").GetString() != "send")
                return;

            JsonElement payload = root.GetProperty("payload");
            if (payload.GetProperty("type").GetString() != "events")
                return;

            byte[] data = e.Data ?? System.Array.Empty<byte>();
            int offset = 0;
            foreach (JsonElement element in payload.GetProperty("events").EnumerateArray())
            {
                int dataSize = element.GetProperty("dataSize").GetInt32();
                byte[] eventData = new byte[dataSize];
                System.Array.Copy(data, offset, eventData, 0, dataSize);
                offset += dataSize;

                Submit(CreateEvent(pid, processName, element, eventData));
            }
        }

        private Event CreateEvent(uint pid, string processName, JsonElement payload, byte[] data)
        {
            Event.ExecutionOrigin executionOrigin = new Event.ExecutionOrigin(processName, pid,
                (uint) payload.GetProperty("threadId").GetInt64());
            Event.InvocationOrigin invocationOrigin = new Event.InvocationOrigin(payload.GetProperty("name").GetString(),
                payload.GetProperty("backtrace").GetString(), (uint) payload.GetProperty("resourceId").GetInt64());

            Event ev;
            if (payload.GetProperty("kind").GetString() == "packet")
            {
                PacketEvent packetEvent = new PacketEvent(eventCoordinator, executionOrigin, invocationOrigin);
                packetEvent.Data = data;
                ev = packetEvent;
            }
            else
            {
                MessageEvent messageEvent = new MessageEvent(eventCoordinator, executionOrigin, invocationOrigin);
                messageEvent.Context = ParseContext(payload.GetProperty("context").GetString());
                messageEvent.SetMessage(payload.GetProperty("text").GetString());
                ev = messageEvent;
            }

            ev.Direction = ParseDirection(payload);
            ev.LocalEndpoint = ParseEndpoint(payload.GetProperty("localAddress").GetString());
            ev.PeerEndpoint = ParseEndpoint(payload.GetProperty("peerAddress").GetString());
            return ev;
        }

        private void Submit(Event ev)
        {
            lock (eventQueue)
            {
                eventQueue.Add(ev);

                if (ev is MessageEvent)
                {
                    eventStats.msgCount++;
                    if (ev.Message != null)
                        eventStats.msgBytes += (uint) (2 * (ev.Message.Length + 1));
                    if (ev.Data != null)
                        eventStats.msgBytes += (uint) ev.Data.Length;
                }
                else
                {
                    eventStats.pktCount++;
                    if (ev.Data != null)
                        eventStats.pktBytes += (uint) ev.Data.Length;
                }
            }

            EventStatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private static PacketDirection ParseDirection(JsonElement payload)
        {
            if (!payload.TryGetProperty("direction", out JsonElement direction))
                return PacketDirection.PACKET_DIRECTION_INVALID;

            switch (direction.GetString())
            {
                case "incoming":
                    return PacketDirection.PACKET_DIRECTION_INCOMING;
                case "outgoing":
                    return PacketDirection.PACKET_DIRECTION_OUTGOING;
                default:
                    return PacketDirection.PACKET_DIRECTION_INVALID;
            }
        }

        private static MessageContext ParseContext(string context)
        {
            switch (context)
            {
                case "warning":
                    return MessageContext.MESSAGE_CTX_WARNING;
                case "error":
                    return MessageContext.MESSAGE_CTX_ERROR;
                case "listening":
                    return MessageContext.MESSAGE_CTX_SOCKET_LISTENING;
                case "connecting":
                    return MessageContext.MESSAGE_CTX_SOCKET_CONNECTING;
                case "connected":
                    return MessageContext.MESSAGE_CTX_SOCKET_CONNECTED;
                case "disconnected":
                    return MessageContext.MESSAGE_CTX_SOCKET_DISCONNECTED;
                case "reset":
                    return MessageContext.MESSAGE_CTX_SOCKET_RESET;
                default:
                    return MessageContext.MESSAGE_CTX_INFO;
            }
        }

        private static IPEndPoint ParseEndpoint(string address)
        {
            if (address.Length == 0)
                return null;

            int separatorIndex = address.LastIndexOf(':');
            IPAddress host = IPAddress.Parse(address.Substring(0, separatorIndex));
            int port = int.Parse(address.Substring(separatorIndex + 1), CultureInfo.InvariantCulture);
            return new IPEndPoint(host, port);
        }

        private static string[] BuildArgv(string fileName, string arguments)
        {
            List<string> argv = new List<string> { fileName };
            argv.AddRange(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return argv.ToArray();
        }

        private static string LoadAgentSource()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("oSpy.Capture.Agent.js");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
