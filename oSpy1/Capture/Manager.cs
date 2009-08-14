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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using EasyHook;

namespace oSpy.Capture
{
    public class Error : Exception
    {
        public Error(string msg)
            : base(msg)
        {
        }

        public Error(string msg, params object[] args)
            : base(String.Format(msg, args))
        {
        }
    }

    public class Manager : MarshalByRefObject
    {
        public delegate void ElementsReceivedHandler(MessageQueueElement[] elements);
        public event ElementsReceivedHandler MessageElementsReceived;

        public const int MAX_ELEMENTS = 2048;
        public const int PACKET_BUFSIZE = 65536;
        public const int MAX_SOFTWALL_RULES = 128;
        public const int BACKTRACE_BUFSIZE = 384;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct MessageQueueElement
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

        public const int SOFTWALL_CONDITION_PROCESS_NAME = 1;
        public const int SOFTWALL_CONDITION_FUNCTION_NAME = 2;
        public const int SOFTWALL_CONDITION_RETURN_ADDRESS = 4;
        public const int SOFTWALL_CONDITION_LOCAL_ADDRESS = 8;
        public const int SOFTWALL_CONDITION_LOCAL_PORT = 16;
        public const int SOFTWALL_CONDITION_REMOTE_ADDRESS = 32;
        public const int SOFTWALL_CONDITION_REMOTE_PORT = 64;

        /* connect() errors */
        public const int WSAEHOSTUNREACH = 10065;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct SoftwallRule
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

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct MessageQueue
        {
            public UInt32 num_softwall_rules;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SOFTWALL_RULES)]
            public SoftwallRule[] softwall_rules;

            public UInt32 num_elements;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ELEMENTS)]
            public MessageQueueElement[] elements;
        };

        private Process[] processes = null;
        private SoftwallRule[] softwallRules = null;

        private IProgressFeedback progress = null;

        private Thread workerThread;

        public Manager()
        {
        }

        public void StartCapture(Process[] processes, SoftwallRule[] softwallRules, IProgressFeedback progress)
        {
            this.processes = processes;
            this.softwallRules = softwallRules;
            this.progress = progress;

            workerThread = new Thread(CaptureThread);
            workerThread.Start();
        }

        public void StopCapture(IProgressFeedback progress)
        {
            this.progress = progress;

            workerThread = null;
        }

        private void CaptureThread()
        {
            try
            {
                DoInjection();

                DoCapture(processes, softwallRules);
            }
            catch (Exception e)
            {
                progress.OperationFailed(e.Message);
                return;
            }

            progress.OperationComplete();
        }

        private void DoCapture(Process[] processes, SoftwallRule[] softwallRules)
        {
            progress.ProgressUpdate("Preparing capture", 100);

            IntPtr queuePtr;

            IntPtr map = WinApi.CreateFileMapping(0xFFFFFFFFu, IntPtr.Zero, WinApi.enumProtect.PAGE_READWRITE,
                                                  0, (uint)Marshal.SizeOf(typeof(MessageQueue)),
                                                  "BadgerPacketQueue");
            if (Marshal.GetLastWin32Error() == WinApi.ERROR_ALREADY_EXISTS)
            {
                map = WinApi.OpenFileMapping(WinApi.enumFileMap.FILE_MAP_WRITE, false, "BadgerPacketQueue");
            }

            queuePtr = WinApi.MapViewOfFile(map, WinApi.enumFileMap.FILE_MAP_WRITE,
                                            0, 0, (uint)Marshal.SizeOf(typeof(MessageQueue)));

            Mutex queueMutex = new Mutex(false, "BadgerQueueMutex");

            AutoResetEvent readyEvent = new AutoResetEvent(false);
            readyEvent.SafeWaitHandle = WinApi.CreateEvent(IntPtr.Zero,
                                                           false, // bManualReset
                                                           false, // bInitialState
                                                           "BadgerPacketReady");

            IntPtr numElementsPtr = (IntPtr)(queuePtr.ToInt64() +
                                             Marshal.OffsetOf(typeof(MessageQueue), "num_elements").ToInt64());
            IntPtr elementsPtr = (IntPtr)(queuePtr.ToInt64() +
                                          Marshal.OffsetOf(typeof(MessageQueue), "elements").ToInt64());

            int elementSize = Marshal.SizeOf(typeof(MessageQueueElement));
            IntPtr tmpElementPtr = Marshal.AllocHGlobal(elementSize);

            try
            {
                //
                // First off, upload softwall rules
                //
                if (!queueMutex.WaitOne(5000, false))
                {
                    progress.OperationFailed("Failed to upload softwall rules");
                    return;
                }

                try
                {
                    // Empty message queue
                    //Marshal.WriteInt32(numElementsPtr, 0);

                    Marshal.WriteIntPtr(queuePtr, Marshal.OffsetOf(typeof(MessageQueue), "num_softwall_rules").ToInt32(),
                                        (IntPtr)softwallRules.Length);

                    IntPtr p = (IntPtr)(queuePtr.ToInt64() +
                        Marshal.OffsetOf(typeof(MessageQueue), "softwall_rules").ToInt64());
                    foreach (SoftwallRule rule in softwallRules)
                    {
                        Marshal.StructureToPtr(rule, p, false);

                        p = (IntPtr)(p.ToInt64() + Marshal.SizeOf(typeof(SoftwallRule)));
                    }
                }
                finally
                {
                    queueMutex.ReleaseMutex();
                }

                progress.OperationComplete();

                //
                // Then start monitoring
                //
                while (workerThread != null)
                {
                    Int32 len;
                    byte[] bytes = null;

                    Thread.Sleep(1000);

                    if (readyEvent.WaitOne(500, false))
                    {
                        if (!queueMutex.WaitOne(5000, false))
                        {
                            progress.OperationFailed("Failed to acquire lock");
                            return;
                        }

                        try
                        {
                            len = Marshal.ReadInt32(numElementsPtr);

                            if (len > 0)
                            {
                                bytes = new byte[len * Marshal.SizeOf(typeof(MessageQueueElement))];

                                Marshal.Copy(elementsPtr, bytes, 0, bytes.Length);

                                Marshal.WriteInt32(numElementsPtr, 0);
                            }
                        }
                        finally
                        {
                            queueMutex.ReleaseMutex();
                        }

                        if (bytes != null)
                        {
                            MessageQueueElement[] elements = new MessageQueueElement[len];

                            for (int i = 0; i < len; i++)
                            {
                                Marshal.Copy(bytes, i * elementSize, tmpElementPtr, elementSize);
                                elements[i] = (MessageQueueElement)Marshal.PtrToStructure(tmpElementPtr, typeof(MessageQueueElement));
                            }

                            MessageElementsReceived(elements);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tmpElementPtr);
            }
        }

        private void DoInjection()
        {
            for (int i = 0; i < processes.Length; i++)
            {
                int percentComplete = (int)(((float)(i + 1) / (float)processes.Length) * 100.0f);
                progress.ProgressUpdate("Injecting logging agents", percentComplete);
                InjectDll(processes[i].Id);
            }
        }

        private void InjectDll(int processId)
        {
            InjectDll(processId, "oSpyAgent.dll");
        }

        private void InjectDll(int processId, string filename)
        {
            string binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            RemoteHooking.Inject(processId, filename, filename, binDir);
        }

        private bool UnInjectDll(int processId, UIntPtr handle)
        {
            // FIXME: need to handle this in the agent
            return true;
        }
    }
}
