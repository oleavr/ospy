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
        public Error (string msg)
            : base (msg)
        {
        }

        public Error (string msg, params object[] args)
            : base (String.Format (msg, args))
        {
        }
    }

    public class Manager
    {
        public const int MAX_SOFTWALL_RULES = 128;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
        public struct Capture
        {
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = WinApi.MAX_PATH)]
            public string LogPath;
            public volatile UInt32 LogIndexUserspace;
            public volatile UInt32 LogCount;
            public volatile UInt32 LogSize;

            public UInt32 NumSoftwallRules;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SOFTWALL_RULES)]
            public Softwall.Rule[] SoftwallRules;
        }

        private Process[] processes = null;
        private Softwall.Rule[] softwallRules = null;
        private Device[] devices = null;

        private UIntPtr[] handles = null;
        private IProgressFeedback progress = null;

        private SafeWaitHandle startEvent;
        private IntPtr fileMapping, cfgPtr;
        private IntPtr logIndexUserspacePtr, logCountPtr, logSizePtr;

        private bool restartDevices;
        public bool RestartDevices
        {
            get { return restartDevices; }
            set { restartDevices = value; }
        }

        private string capturePath;
        public string CapturePath
        {
            get { return capturePath; }
        }

        private int eventCount;
        public int EventCount
        {
            get { return eventCount; }
        }

        private int captureSize;
        public int CaptureSize
        {
            get { return captureSize; }
        }

        public string UsbAgentServiceInstallPath
        {
            get { return Path.Combine (Path.GetDirectoryName (Environment.GetFolderPath (Environment.SpecialFolder.System)), Constants.UsbAgentPath); }
        }

        public Manager()
        {
        }

        public void StartCapture(Process[] processes, Softwall.Rule[] softwallRules, Device[] devices, IProgressFeedback progress)
        {
            this.processes = processes;
            this.softwallRules = softwallRules;
            this.devices = devices;
            this.progress = progress;

            Thread th = new Thread(StartCaptureThread);
            th.Start();
        }

        public void StopCapture(IProgressFeedback progress)
        {
            this.progress = progress;

            Thread th = new Thread(StopCaptureThread);
            th.Start();
        }

        public void CloseCapture()
        {
            Directory.Delete(capturePath, true);
        }

        public void UpdateCaptureStatistics()
        {
            eventCount = Marshal.ReadInt32(logCountPtr);
            captureSize = Marshal.ReadInt32(logSizePtr);
        }

        private void StartCaptureThread()
        {
            try
            {
                PrepareCapture (processes, softwallRules);

                if (devices.Length > 0)
                {
                    InstallUsbAgentService ();

                    foreach (Device device in devices)
                        device.AddLowerFilter (Constants.UsbAgentName);

                    if (restartDevices)
                    {
                        foreach (Device device in devices)
                            device.Restart ();
                    }
                }

                DoInjection ();
            }
            catch (Error e)
            {
                // TODO: roll back

                progress.OperationFailed (e.Message);
                return;
            }

            progress.OperationComplete();
        }

        private void StopCaptureThread ()
        {
            try
            {
                DoUnInjection ();

                UpdateCaptureStatistics ();

                startEvent.Close ();

                WinApi.UnmapViewOfFile (cfgPtr);
                WinApi.CloseHandle (fileMapping);

                if (devices.Length > 0)
                {
                    foreach (Device device in devices)
                        device.RemoveLowerFilter (Constants.UsbAgentName);

                    if (restartDevices)
                    {
                        foreach (Device device in devices)
                            device.Restart ();
                    }

                    WaitForUsbAgentServiceToStop ();

                    RemoveUsbAgentService ();
                }
            }
            catch (Error e)
            {
                progress.OperationFailed(e.Message);
                return;
            }

            progress.OperationComplete();
        }

        private void InstallUsbAgentService ()
        {
            string binDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
            string srcFile = Path.Combine (binDir, Constants.UsbAgentFilename);

            try
            {
                File.Copy (srcFile, UsbAgentServiceInstallPath, true);
            }
            catch (Exception e)
            {
                throw new Error ("Failed to install USB agent driver: {0}", e.Message);
            }

            IntPtr manager = WinApi.OpenSCManager (null, null, WinApi.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                throw new Error ("OpenSCManager failed");

            IntPtr service = IntPtr.Zero;

            try
            {
                service = WinApi.OpenService (manager, Constants.UsbAgentName, WinApi.SERVICE_ALL_ACCESS);
                if (service == IntPtr.Zero && Marshal.GetLastWin32Error () == WinApi.ERROR_SERVICE_DOES_NOT_EXIST)
                {
                    service = WinApi.CreateService (manager, Constants.UsbAgentName, Constants.UsbAgentDescription, WinApi.SERVICE_ALL_ACCESS, WinApi.SERVICE_KERNEL_DRIVER, WinApi.SERVICE_DEMAND_START, WinApi.SERVICE_ERROR_NORMAL, Constants.UsbAgentPath, null, 0, null, null, null);
                    if (service == IntPtr.Zero)
                        throw new Error ("CreateService failed");
                }

                if (!WinApi.StartService (service, 0, null))
                {
                    int lastError = Marshal.GetLastWin32Error ();
                    if (lastError != WinApi.ERROR_SERVICE_ALREADY_RUNNING && lastError != WinApi.ERROR_SERVICE_DISABLED)
                        throw new Error ("Failed to start service: 0x{0:x8}", lastError);
                }
            }
            finally
            {
                if (service != IntPtr.Zero)
                    WinApi.CloseServiceHandle (service);

                WinApi.CloseServiceHandle (manager);
            }
        }

        private void RemoveUsbAgentService ()
        {
            IntPtr manager = WinApi.OpenSCManager (null, null, WinApi.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                throw new Error ("OpenSCManager failed");

            IntPtr service = IntPtr.Zero;

            try
            {
                service = WinApi.OpenService (manager, Constants.UsbAgentName, WinApi.SERVICE_ALL_ACCESS);
                if (service != IntPtr.Zero)
                {
                    if (!WinApi.DeleteService (service))
                        throw new Error ("DeleteService failed: 0x{0:x8}", Marshal.GetLastWin32Error ());
                }
                else
                {
                    if (Marshal.GetLastWin32Error () != WinApi.ERROR_SERVICE_DOES_NOT_EXIST)
                        throw new Error ("OpenService failed");
                }
            }
            finally
            {
                if (service != IntPtr.Zero)
                    WinApi.CloseServiceHandle (service);

                WinApi.CloseServiceHandle (manager);
            }

            if (File.Exists (UsbAgentServiceInstallPath))
                File.Delete (UsbAgentServiceInstallPath);
        }

        private void WaitForUsbAgentServiceToStop ()
        {
            IntPtr manager = WinApi.OpenSCManager (null, null, WinApi.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                throw new Error ("OpenSCManager failed");

            IntPtr service = IntPtr.Zero;

            try
            {
                service = WinApi.OpenService (manager, Constants.UsbAgentName, WinApi.SERVICE_ALL_ACCESS);
                if (service == IntPtr.Zero)
                    throw new Error ("OpenService failed");

                WinApi.SERVICE_STATUS status = new WinApi.SERVICE_STATUS ();

                progress.ProgressUpdate ("Unplug any USB device being monitored now", 100);

                bool stopped = false;

                while (!stopped)
                {
                    if (!WinApi.QueryServiceStatus (service, ref status))
                        throw new Error ("Failed to query for service status: 0x{0:x8}", Marshal.GetLastWin32Error ());

                    stopped = status.dwCurrentState == WinApi.SERVICE_STOPPED;
                    if (!stopped)
                        Thread.Sleep (250);
                }
            }
            finally
            {
                if (service != IntPtr.Zero)
                    WinApi.CloseServiceHandle (service);

                WinApi.CloseServiceHandle (manager);
            }
        }

        private void PrepareCapture(Process[] processes, Softwall.Rule[] softwallRules)
        {
            progress.ProgressUpdate("Preparing capture", 100);

            startEvent = WinApi.CreateEvent (IntPtr.Zero, true, false, "oSpyAgentStartEvent");

            fileMapping = WinApi.CreateFileMapping (0xFFFFFFFFu, IntPtr.Zero,
                WinApi.enumProtect.PAGE_READWRITE,
                0, (uint)Marshal.SizeOf(typeof(Capture)),
                "Global\\oSpyCapture");
            if (Marshal.GetLastWin32Error () == WinApi.ERROR_ALREADY_EXISTS)
                throw new Error("Is another instance of oSpy or one or more processes previously monitored still alive?");

            cfgPtr = WinApi.MapViewOfFile (fileMapping, WinApi.enumFileMap.FILE_MAP_WRITE, 0, 0, (uint)Marshal.SizeOf (typeof (Capture)));

            // Create a temporary directory for the capture
            do
            {
                capturePath = String.Format("{0}{1}", Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(capturePath));

            Directory.CreateDirectory(capturePath);

            // Write the temporary directory to shared memory
            char[] tmpDirChars = capturePath.ToCharArray();
            IntPtr ptr = (IntPtr)(cfgPtr.ToInt64() + Marshal.OffsetOf(typeof(Capture), "LogPath").ToInt64());
            Marshal.Copy(tmpDirChars, 0, ptr, tmpDirChars.Length);

            // And make it NUL-terminated
            Marshal.WriteInt16(ptr, tmpDirChars.Length * Marshal.SizeOf(typeof(UInt16)), 0);

            // Initialize LogIndex and LogSize
            logIndexUserspacePtr = (IntPtr)(cfgPtr.ToInt64 () + Marshal.OffsetOf (typeof (Capture), "LogIndexUserspace").ToInt64 ());
            logCountPtr = (IntPtr)(cfgPtr.ToInt64 () + Marshal.OffsetOf (typeof (Capture), "LogCount").ToInt64 ());
            logSizePtr = (IntPtr)(cfgPtr.ToInt64 () + Marshal.OffsetOf (typeof (Capture), "LogSize").ToInt64 ());

            Marshal.WriteInt32 (logIndexUserspacePtr, 0);
            Marshal.WriteInt32 (logCountPtr, 0);
            Marshal.WriteInt32 (logSizePtr, 0);

            // Initialize softwall rules
            Marshal.WriteInt32(cfgPtr, Marshal.OffsetOf(typeof(Capture), "NumSoftwallRules").ToInt32(), softwallRules.Length);

            ptr = (IntPtr)(cfgPtr.ToInt64() + Marshal.OffsetOf(typeof(Capture), "SoftwallRules").ToInt64());
            foreach (Softwall.Rule rule in softwallRules)
            {
                Marshal.StructureToPtr(rule, ptr, false);

                ptr = (IntPtr)(ptr.ToInt64() + Marshal.SizeOf(typeof(Softwall.Rule)));
            }

            // Copy configuration XML
            string configPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\config.xml";
            File.Copy(configPath, String.Format("{0}\\config.xml", capturePath));
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
                    progress.ProgressUpdate ("Injecting logging agents", percentComplete);
                    handles[i] = InjectDll (processes[i].Id);
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
                            UnInjectDll (processes[i].Id, handles[i]);
                        }
                        catch
                        {
                        }
                    }
                }

                throw;
            }

            WinApi.SetEvent (startEvent);
        }

        private void DoUnInjection ()
        {
            // Uninject the DLLs previously injected
            for (int i = 0; i < processes.Length; i++)
            {
                int percentComplete = (int)(((float)(i + 1) / (float)processes.Length) * 100.0f);
                progress.ProgressUpdate("Uninjecting logging agents", percentComplete);

                if (!processes[i].HasExited)
                {
                    if (!UnInjectDll (processes[i].Id, handles[i]))
                    {
                        // TODO: warning here?
                    }
                }
            }
        }

        private UIntPtr InjectDll(int processId)
        {
            string agentDLLPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\oSpyAgent.dll";
            if (!File.Exists(agentDLLPath))
                throw new Error(agentDLLPath + " not found");

            return InjectDll(processId, agentDLLPath);
        }

        private UIntPtr InjectDll (int processId, string dllPath)
        {
            IntPtr kernelMod = WinApi.GetModuleHandle ("kernel32.dll");
            if (kernelMod == IntPtr.Zero)
                throw new Error ("GetModuleHandle of kernel32.dll failed");

            IntPtr proc = WinApi.OpenProcess (WinApi.PROCESS_ALL_ACCESS, true, (uint)processId);
            if (proc == IntPtr.Zero)
                throw new Error ("OpenProcess failed");

            // Temporarily show errors, add the directory to the search path, and load the DLL.
            try
            {
                uint oldErrorMode = CallRemoteFunction (proc, kernelMod, "SetErrorMode", 0);

                try
                {
                    string dllDir = Path.GetDirectoryName (dllPath);
                    CallRemoteFunction (proc, kernelMod, "SetDllDirectoryW", dllDir);
                    return (UIntPtr) CallRemoteFunction (proc, kernelMod, "LoadLibraryW", dllPath);
                }
                finally
                {
                    CallRemoteFunction (proc, kernelMod, "SetErrorMode", oldErrorMode);
                }
            }
            finally
            {
                WinApi.CloseHandle (proc);
            }
        }

        private bool UnInjectDll (int processId, UIntPtr handle)
        {
            IntPtr kernelMod = WinApi.GetModuleHandle ("kernel32.dll");
            if (kernelMod == IntPtr.Zero)
                throw new Error ("GetModuleHandle of kernel32.dll failed");

            IntPtr proc = WinApi.OpenProcess (WinApi.PROCESS_ALL_ACCESS, true, (uint)processId);
            if (proc == IntPtr.Zero)
                throw new Error ("OpenProcess failed");

            try
            {
                return CallRemoteFunction (proc, kernelMod, "FreeLibrary", handle) != 0;
            }
            finally
            {
                WinApi.CloseHandle (proc);
            }
        }

        private uint CallRemoteFunction (IntPtr proc, IntPtr kernelMod, string funcName, uint argument)
        {
            return CallRemoteFunction (proc, kernelMod, funcName, (UIntPtr) argument);
        }

        private uint CallRemoteFunction (IntPtr proc, IntPtr kernelMod, string funcName, string argument)
        {
            UIntPtr remoteArg = AllocateRemoteString (proc, argument);

            try
            {
                return CallRemoteFunction (proc, kernelMod, funcName, remoteArg);
            }
            finally
            {
                // TODO: free the remote string here
            }
        }

        private uint CallRemoteFunction (IntPtr proc, IntPtr kernelMod, string funcName, UIntPtr argument)
        {
            IntPtr funcAddr = WinApi.GetProcAddress (kernelMod, funcName);
            if (funcAddr == IntPtr.Zero)
                throw new Error (String.Format ("GetProcAddress of {0} failed", funcName));

            IntPtr remoteThreadHandle = WinApi.CreateRemoteThread (proc, IntPtr.Zero, 0, funcAddr, argument, 0, IntPtr.Zero);
            if (remoteThreadHandle == IntPtr.Zero)
                throw new Error ("CreateRemoteThread failed");

            return GetThreadExitCode (remoteThreadHandle);
        }

        private uint GetThreadExitCode (IntPtr handle)
        {
            while (true)
            {
                uint exitCode;
                if (!WinApi.GetExitCodeThread (handle, out exitCode))
                    throw new Error ("GetExitCodeThread failed");

                if (exitCode != WinApi.STILL_ACTIVE)
                    return exitCode;
                else
                    Thread.Sleep (100);
            }
        }

        private UIntPtr[] GetThreadExitCodes (IntPtr[] handles, string progressMsg)
        {
            Dictionary<IntPtr, UIntPtr> exitCodes = new Dictionary<IntPtr, UIntPtr> (handles.Length);
            List<IntPtr> pendingHandles = new List<IntPtr> (handles);
            List<IntPtr> completedHandles = new List<IntPtr> (handles.Length);

            while (pendingHandles.Count > 0)
            {
                int completionCount = processes.Length - pendingHandles.Count + 1;
                int percentComplete = (int)(((float)completionCount / (float)processes.Length) * 100.0f);

                progress.ProgressUpdate (progressMsg, percentComplete);

                completedHandles.Clear ();

                for (int i = 0; i < pendingHandles.Count; i++)
                {
                    uint exitCode;
                    if (!WinApi.GetExitCodeThread (pendingHandles[i], out exitCode))
                        throw new Error ("GetExitCodeThread failed");

                    if (exitCode != WinApi.STILL_ACTIVE)
                    {
                        exitCodes[pendingHandles[i]] = (UIntPtr)exitCode;
                        completedHandles.Add (pendingHandles[i]);
                    }
                }

                foreach (IntPtr handle in completedHandles)
                {
                    pendingHandles.Remove (handle);
                }

                Thread.Sleep (200);
            }

            UIntPtr[] result = new UIntPtr[handles.Length];
            for (int i = 0; i < handles.Length; i++)
            {
                result[i] = exitCodes[handles[i]];
            }
            return result;
        }

        private UIntPtr AllocateRemoteString (IntPtr proc, string str)
        {
            // Create a unicode (UTF-16) C-string
            byte[] rawStr = Encoding.Unicode.GetBytes (str);
            byte[] termStr = new byte[rawStr.Length + 2];
            rawStr.CopyTo (termStr, 0);
            termStr[termStr.Length - 2] = 0;
            termStr[termStr.Length - 1] = 0;

            // Allocate memory for the string in the target process
            UIntPtr remoteStr = WinApi.VirtualAllocEx (proc, IntPtr.Zero,
                (uint)termStr.Length, WinApi.MEM_COMMIT, WinApi.PAGE_READWRITE);
            if (remoteStr == UIntPtr.Zero)
                throw new Error ("VirtualAllocEx failed");

            // Write the string to the allocated buffer
            IntPtr bytesWritten;
            if (!WinApi.WriteProcessMemory (proc, remoteStr, termStr, (uint)termStr.Length, out bytesWritten))
                throw new Error ("WriteProcessMemory failed");

            return remoteStr;
        }
    }
}
