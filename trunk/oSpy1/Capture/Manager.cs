//
// Copyright (c) 2007-2008 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using EasyHook;

namespace oSpy.Capture
{
    public interface IManager
    {
        void Submit(MessageQueueElement[] elements);
        void Ping(int pid);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
    public class MessageQueueElement
    {
        /* Common fields */
        public WinApi.SYSTEMTIME time;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string process_name;
        public UInt32 process_id;
        public UInt32 thread_id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string function_name;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Manager.BACKTRACE_BUFSIZE)]
        public string backtrace;

        public UInt32 resource_id;

        public MessageType msg_type;

        /* MessageType.Message */
        public MessageContext context;
        public UInt32 domain;
        public UInt32 severity;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string message;

        /* MessageType.Packet */
        public PacketDirection direction;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string local_address;
        public UInt32 local_port;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string peer_address;
        public UInt32 peer_port;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Manager.PACKET_BUFSIZE)]
        public byte[] buf;
        public UInt32 len;
    };

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

    public class StartDetails : Details
    {
        private ProcessStartInfo info;

        public ProcessStartInfo Info
        {
            get
            {
                return info;
            }
        }

        public StartDetails(ProcessStartInfo info)
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

    public class Manager : MarshalByRefObject, IManager
    {
        public delegate void ElementsReceivedHandler(MessageQueueElement[] elements);
        public event ElementsReceivedHandler MessageElementsReceived;

        private const string AGENT_DLL = "oSpyAgent.dll";

        public const int PACKET_BUFSIZE = 65536;
        public const int BACKTRACE_BUFSIZE = 384;

        public const int SOFTWALL_CONDITION_PROCESS_NAME = 1;
        public const int SOFTWALL_CONDITION_FUNCTION_NAME = 2;
        public const int SOFTWALL_CONDITION_RETURN_ADDRESS = 4;
        public const int SOFTWALL_CONDITION_LOCAL_ADDRESS = 8;
        public const int SOFTWALL_CONDITION_LOCAL_PORT = 16;
        public const int SOFTWALL_CONDITION_REMOTE_ADDRESS = 32;
        public const int SOFTWALL_CONDITION_REMOTE_PORT = 64;

        /* connect() errors */
        public const int WSAEHOSTUNREACH = 10065;

        private Details details = null;
        private IProgressFeedback progress = null;

        private Thread startWorkerThread;

        private IpcServerChannel serverChannel;
        private string serverChannelName;
        private List<int> clients;
        private AutoResetEvent clientAdded = new AutoResetEvent(false);
        private ManualResetEvent stopRequest = new ManualResetEvent(false);

        public Manager()
        {
        }

        public override object InitializeLifetimeService()
        {
            return null; // live forever
        }

        public void Submit(MessageQueueElement[] elements)
        {
            lock (MessageElementsReceived)
            {
                MessageElementsReceived(elements);
            }
        }

        public void Ping(int pid)
        {
            lock (clients)
            {
                if (!clients.Contains(pid))
                {
                    clients.Add(pid);
                    clientAdded.Set();
                }
            }
        }

        public void StartCapture(Details details, IProgressFeedback progress)
        {
            this.details = details;
            this.progress = progress;

            stopRequest.Reset();

            startWorkerThread = new Thread(DoStartCapture);
            startWorkerThread.Start();
        }

        public void StopCapture(IProgressFeedback progress)
        {
            stopRequest.Set();

            // Unlikely:
            while (startWorkerThread != null)
                Thread.Sleep(20);

            this.progress = progress;

            DoStopCapture();
        }

        private void DoStartCapture()
        {
            try
            {
                PrepareCapture();

                int[] processIds;

                if (details is StartDetails)
                {
                    StartDetails startDetails = details as StartDetails;
                    int processId = DoCreation(startDetails);
                    processIds = new int[1] { processId };
                }
                else if (details is AttachDetails)
                {
                    AttachDetails attachDetails = details as AttachDetails;
                    DoInjection(attachDetails);
                    processIds = attachDetails.ProcessIds;
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (WaitForAllClientsToPingUs(processIds))
                    progress.OperationComplete();
                else
                    progress.OperationFailed("Capture aborted.");
            }
            catch (Exception e)
            {
                progress.OperationFailed(e.Message);
            }

            progress = null;
            startWorkerThread = null;
        }

        private void DoStopCapture()
        {
            RemotingServices.Disconnect(this);
            ChannelServices.UnregisterChannel(serverChannel);

            serverChannel = null;
            serverChannelName = null;
            clients = null;
            clientAdded.Reset();
            stopRequest.Reset();

            details = null;

            progress.OperationComplete();
            progress = null;
        }

        private void PrepareCapture()
        {
            serverChannelName = GenerateChannelName();
            serverChannel = CreateServerChannel(serverChannelName);
            clients = new List<int>();

            ChannelServices.RegisterChannel(serverChannel, false);
            RemotingServices.Marshal(this, serverChannelName, typeof(IManager));
        }

        private int DoCreation(StartDetails startDetails)
        {
            progress.ProgressUpdate("Starting process and injecting logging agent", 100);

            ProcessStartInfo psi = startDetails.Info;
            int processId;
            RemoteHooking.CreateAndInject(psi.FileName,
                (psi.Arguments != String.Empty) ? psi.Arguments : null,
                (psi.WorkingDirectory != String.Empty) ? psi.WorkingDirectory : null,
                0, AGENT_DLL, AGENT_DLL, out processId,
                serverChannelName, details.SoftwallRules);

            return processId;
        }

        private void DoInjection(AttachDetails attachDetails)
        {
            Process[] processes = attachDetails.Processes;
            for (int i = 0; i < processes.Length; i++)
            {
                int percentComplete = (int)(((float)(i + 1) / (float)processes.Length) * 100.0f);
                progress.ProgressUpdate("Injecting logging agents", percentComplete);
                RemoteHooking.Inject(processes[i].Id, AGENT_DLL, AGENT_DLL,
                    serverChannelName, details.SoftwallRules);
            }
        }

        private bool WaitForAllClientsToPingUs(int[] clientProcessIds)
        {
            WaitHandle[] waitHandles = { stopRequest, clientAdded };

            while (true)
            {
                lock (clients)
                {
                    if (clients.Count == clientProcessIds.Length)
                        return true;
                }

                int index = WaitHandle.WaitAny(waitHandles, 1000);
                if (index == 0)
                    return false;

                foreach (int clientPid in clientProcessIds)
                    Process.GetProcessById(clientPid); // throws an ArgumentException if the process is no longer around
            }
        }

        // These two are based on similar utility methods in EasyHook:
        private static IpcServerChannel CreateServerChannel(string channelName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["name"] = channelName;
            properties["portName"] = channelName;

            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(
                AccessControlType.Allow,
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                -1,
                InheritanceFlags.None,
                PropagationFlags.None);

            CommonSecurityDescriptor secDesc = new CommonSecurityDescriptor(
                false,
                false,
                ControlFlags.GroupDefaulted | ControlFlags.OwnerDefaulted | ControlFlags.DiscretionaryAclPresent,
                null,
                null,
                null,
                dacl);

            BinaryServerFormatterSinkProvider sinkProv = new BinaryServerFormatterSinkProvider();
            sinkProv.TypeFilterLevel = TypeFilterLevel.Full;

            return new IpcServerChannel(properties, sinkProv, secDesc);
        }

        private static string GenerateChannelName()
        {
            byte[] data = new byte[30];
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(data);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 20 + data[0] % 10; i++)
            {
                byte b = (byte) (data[i] % 62);

                if (b >= 0 && b <= 9)
                    builder.Append((char) ('0' + b));
                else if (b >= 10 && b <= 35)
                    builder.Append((char) ('A' + (b - 10)));
                else if (b >= 36 && b <= 61)
                    builder.Append((char) ('a' + (b - 36)));
            }

            return builder.ToString();
        }
    }
}
