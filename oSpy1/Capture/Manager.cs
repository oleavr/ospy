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
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32.SafeHandles;

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

    public class Manager
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

        private UIntPtr[] handles = null;
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
            catch (Error e)
            {
                // TODO: roll back

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
            handles = new UIntPtr[processes.Length];

            try
            {
                // Inject into the desired process IDs
                for (int i = 0; i < processes.Length; i++)
                {
                    int percentComplete = (int)(((float)(i + 1) / (float)processes.Length) * 100.0f);
                    progress.ProgressUpdate("Injecting logging agents", percentComplete);
                    handles[i] = InjectDll(processes[i].Id);
                }
            }
            catch
            {
                // Roll back if needed
                for (int i = 0; i < handles.Length; i++)
                {
                    if (handles[i] != UIntPtr.Zero)
                    {
                        try
                        {
                            UnInjectDll(processes[i].Id, handles[i]);
                        }
                        catch
                        {
                        }
                    }
                }

                throw;
            }
        }

        private UIntPtr InjectDll(int processId)
        {
            string agentDLLPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\oSpyAgent.dll";
            if (!File.Exists(agentDLLPath))
                throw new Error(agentDLLPath + " not found");

            return InjectDll(processId, agentDLLPath);
        }

        private UIntPtr InjectDll(int processId, string dllPath)
        {
            IntPtr kernelMod = WinApi.GetModuleHandle("kernel32.dll");
            if (kernelMod == IntPtr.Zero)
                throw new Error("GetModuleHandle of kernel32.dll failed");

            IntPtr proc = WinApi.OpenProcess(WinApi.PROCESS_ALL_ACCESS, true, (uint)processId);
            if (proc == IntPtr.Zero)
                throw new Error("OpenProcess failed");

            // Temporarily show errors, add the directory to the search path, and load the DLL.
            try
            {
                uint oldErrorMode = CallRemoteFunction(proc, kernelMod, "SetErrorMode", 0);

                try
                {
                    string dllDir = Path.GetDirectoryName(dllPath);
                    CallRemoteFunction(proc, kernelMod, "SetDllDirectoryW", dllDir);
                    UIntPtr dllHandle = (UIntPtr) CallRemoteFunction(proc, kernelMod, "LoadLibraryW", dllPath);
                    if (dllHandle == UIntPtr.Zero)
                        throw new Error("LoadLibrary in remote process failed");
                    return dllHandle;
                }
                finally
                {
                    CallRemoteFunction(proc, kernelMod, "SetErrorMode", oldErrorMode);
                }
            }
            finally
            {
                WinApi.CloseHandle(proc);
            }
        }

        private bool UnInjectDll(int processId, UIntPtr handle)
        {
            // FIXME: need to handle this in the agent
#if false
            IntPtr kernelMod = WinApi.GetModuleHandle("kernel32.dll");
            if (kernelMod == IntPtr.Zero)
                throw new Error("GetModuleHandle of kernel32.dll failed");

            IntPtr proc = WinApi.OpenProcess(WinApi.PROCESS_ALL_ACCESS, true, (uint)processId);
            if (proc == IntPtr.Zero)
                throw new Error("OpenProcess failed");

            try
            {
                return CallRemoteFunction(proc, kernelMod, "FreeLibrary", handle) != 0;
            }
            finally
            {
                WinApi.CloseHandle(proc);
            }
#endif
            return true;
        }

        private uint CallRemoteFunction(IntPtr proc, IntPtr kernelMod, string funcName, uint argument)
        {
            return CallRemoteFunction(proc, kernelMod, funcName, (UIntPtr)argument);
        }

        private uint CallRemoteFunction(IntPtr proc, IntPtr kernelMod, string funcName, string argument)
        {
            UIntPtr remoteArg = AllocateRemoteString(proc, argument);

            try
            {
                return CallRemoteFunction(proc, kernelMod, funcName, remoteArg);
            }
            finally
            {
                // TODO: free the remote string here
            }
        }

        private uint CallRemoteFunction(IntPtr proc, IntPtr kernelMod, string funcName, UIntPtr argument)
        {
            IntPtr funcAddr = WinApi.GetProcAddress(kernelMod, funcName);
            if (funcAddr == IntPtr.Zero)
                throw new Error(String.Format("GetProcAddress of {0} failed", funcName));

            IntPtr remoteThreadHandle = WinApi.CreateRemoteThread(proc, IntPtr.Zero, 0, funcAddr, argument, 0, IntPtr.Zero);
            if (remoteThreadHandle == IntPtr.Zero)
                throw new Error("CreateRemoteThread failed");

            return GetThreadExitCode(remoteThreadHandle);
        }

        private uint GetThreadExitCode(IntPtr handle)
        {
            while (true)
            {
                uint exitCode;
                if (!WinApi.GetExitCodeThread(handle, out exitCode))
                    throw new Error("GetExitCodeThread failed");

                if (exitCode != WinApi.STILL_ACTIVE)
                    return exitCode;
                else
                    Thread.Sleep(100);
            }
        }

        private UIntPtr[] GetThreadExitCodes(IntPtr[] handles, string progressMsg)
        {
            Dictionary<IntPtr, UIntPtr> exitCodes = new Dictionary<IntPtr, UIntPtr>(handles.Length);
            List<IntPtr> pendingHandles = new List<IntPtr>(handles);
            List<IntPtr> completedHandles = new List<IntPtr>(handles.Length);

            while (pendingHandles.Count > 0)
            {
                int completionCount = processes.Length - pendingHandles.Count + 1;
                int percentComplete = (int)(((float)completionCount / (float)processes.Length) * 100.0f);

                progress.ProgressUpdate(progressMsg, percentComplete);

                completedHandles.Clear();

                for (int i = 0; i < pendingHandles.Count; i++)
                {
                    uint exitCode;
                    if (!WinApi.GetExitCodeThread(pendingHandles[i], out exitCode))
                        throw new Error("GetExitCodeThread failed");

                    if (exitCode != WinApi.STILL_ACTIVE)
                    {
                        exitCodes[pendingHandles[i]] = (UIntPtr)exitCode;
                        completedHandles.Add(pendingHandles[i]);
                    }
                }

                foreach (IntPtr handle in completedHandles)
                {
                    pendingHandles.Remove(handle);
                }

                Thread.Sleep(200);
            }

            UIntPtr[] result = new UIntPtr[handles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                result[i] = exitCodes[handles[i]];
            }
            return result;
        }

        private UIntPtr AllocateRemoteString(IntPtr proc, string str)
        {
            // Create a unicode (UTF-16) C-string
            byte[] rawStr = Encoding.Unicode.GetBytes(str);
            byte[] termStr = new byte[rawStr.Length + 2];
            rawStr.CopyTo(termStr, 0);
            termStr[termStr.Length - 2] = 0;
            termStr[termStr.Length - 1] = 0;

            // Allocate memory for the string in the target process
            UIntPtr remoteStr = WinApi.VirtualAllocEx(proc, IntPtr.Zero,
                (uint)termStr.Length, WinApi.MEM_COMMIT, WinApi.PAGE_READWRITE);
            if (remoteStr == UIntPtr.Zero)
                throw new Error("VirtualAllocEx failed");

            // Write the string to the allocated buffer
            IntPtr bytesWritten;
            if (!WinApi.WriteProcessMemory(proc, remoteStr, termStr, (uint)termStr.Length, out bytesWritten))
                throw new Error("WriteProcessMemory failed");

            return remoteStr;
        }
    }
}
