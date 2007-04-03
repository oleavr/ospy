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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace oSpy
{
    public partial class CaptureChooseForm : Form
    {
        private int checkCount = 0;

        public CaptureChooseForm()
        {
            InitializeComponent();
        }

        private void InjectForm_Shown(object sender, EventArgs e)
        {
            UpdateProcessList();
            UpdateButtons();
        }

        private void UpdateProcessList()
        {
            List<ProcessItem> items = new List<ProcessItem>();

            int ourSessionId = Process.GetCurrentProcess().SessionId;

            foreach (Process proc in Process.GetProcesses())
            {
                if (proc.SessionId == ourSessionId)
                {
                    items.Add(new ProcessItem(proc));
                }
            }

            items.Sort();

            processList.Items.Clear();
            processList.Items.AddRange(items.ToArray());
        }

        private void UpdateButtons()
        {
            startBtn.Enabled = (checkCount > 0);
        }

        private void processList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                checkCount++;
            else
                checkCount--;

            UpdateButtons();
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        public int[] GetProcessIds()
        {
            List<int> result = new List<int>();

            if (ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < processList.Items.Count; i++)
                {
                    if (processList.GetItemChecked(i))
                    {
                        result.Add((processList.Items[i] as ProcessItem).Process.Id);
                    }
                }
            }

            return result.ToArray();
        }

#if false
        private const int PROCESS_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF);
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int SYNCHRONIZE = 0x100000;

        private const int MEM_COMMIT = 0x1000;
        private const int PAGE_READWRITE = 0x4;

        private const int STILL_ACTIVE = STATUS_PENDING;
        private const int STATUS_PENDING = 0x103;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle,
           uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
           byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
            IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        private string GetDirName(string path)
        {
            string dirName = "";
            string[] tokens = path.Split('\\');

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                dirName += tokens[i] + '\\';
            }

            return dirName;
        }

        private void injectButton_Click(object sender, EventArgs e)
        {
            if (processView.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem item in processView.SelectedItems)
            {
                Process proc = item.Tag as Process;

                if (InjectDLL(proc.Id))
                {
                    MessageBox.Show(this, String.Format("Agent successfully injected into {0}.", proc.ProcessName),
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    closeButton.Focus();
                }
            }
        }

        private bool InjectDLL(int processId)
        {
            string agentDLLPath = GetDirName(Process.GetCurrentProcess().MainModule.FileName) +
                "oSpyAgent.dll";

            return InjectDLL(processId, agentDLLPath);
        }

        private bool InjectDLL(int processId, string dllPath)
        {
            if (!File.Exists(dllPath))
            {
                MessageBox.Show(this, dllPath + " does not exist", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Create a unicode (UTF-16) C-string
            UnicodeEncoding enc = new UnicodeEncoding();
            byte[] rawdllStr = enc.GetBytes(dllPath);
            byte[] dllStr = new byte[rawdllStr.Length + 2];
            rawdllStr.CopyTo(dllStr, 0);
            dllStr[dllStr.Length - 2] = 0;
            dllStr[dllStr.Length - 1] = 0;

            // Get offset of LoadLibraryW in kernel32
            IntPtr kernelMod = LoadLibrary("kernel32.dll");
            if (kernelMod == IntPtr.Zero)
            {
                MessageBox.Show(this, "LoadLibrary of kernel32.dll failed with error code " +
                                Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                IntPtr loadLibraryAddr = GetProcAddress(kernelMod, "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    MessageBox.Show(this, "GetProcAddress of LoadLibraryW failed with error code " +
                                    Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                    "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Open the target process
                IntPtr proc = OpenProcess(PROCESS_ALL_ACCESS, true, (uint) processId);
                if (proc == IntPtr.Zero)
                {
                    MessageBox.Show(this, "OpenProcess failed with error code " +
                                    Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                    "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                try
                {
                    // Allocate memory for the string in the target process
                    IntPtr remoteDllStr = VirtualAllocEx(proc, IntPtr.Zero,
                        (uint) dllStr.Length, MEM_COMMIT, PAGE_READWRITE);
                    if (remoteDllStr == IntPtr.Zero)
                    {
                        MessageBox.Show(this, "VirtualAllocEx failed with error code " +
                                        Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                        "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Write the string to the allocated buffer
                    IntPtr bytesWritten;
                    if (!WriteProcessMemory(proc, remoteDllStr, dllStr, (uint) dllStr.Length, out bytesWritten))
                    {
                        MessageBox.Show(this, "WriteProcessMemory failed with error code " +
                                        Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                        "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Launch the thread, being LoadLibraryW
                    IntPtr remoteThreadHandle = CreateRemoteThread(proc, IntPtr.Zero, 0, loadLibraryAddr, remoteDllStr, 0, IntPtr.Zero);
                    if (remoteThreadHandle == IntPtr.Zero)
                    {
                        MessageBox.Show(this, "CreateRemoteThread failed with error code " +
                                        Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                        "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Wait for the thread (LoadLibraryW) to return
                    uint exitCode;

                    while (true)
                    {

                        if (!GetExitCodeThread(remoteThreadHandle, out exitCode))
                        {
                            MessageBox.Show(this, "GetExitCodeThread failed with error code " +
                                            Convert.ToString(Marshal.GetLastWin32Error()) + ".",
                                            "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }

                        if (exitCode != STILL_ACTIVE)
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }

                    if (exitCode == 0)
                    {
                        MessageBox.Show(this, "LoadLibraryW in remote process failed.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
                finally
                {
                    CloseHandle(proc);
                }
            }
            finally
            {
                FreeLibrary(kernelMod);
            }
        }

        private void asStartButton_Click(object sender, EventArgs e)
        {
            Process activeSync = new Process();

            activeSync.StartInfo.FileName = String.Format("{0}\\{1}\\WCESMgr.exe",
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft ActiveSync");

            activeSync.Start();

            Thread.Sleep(1000);

            RefreshList();
        }
#endif
    }

    public class ProcessItem : IComparable
    {
        protected Process process;
        public Process Process
        {
            get { return process; }
        }

        public ProcessItem(Process process)
        {
            this.process = process;
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", process.ProcessName, process.Id);
        }

        public int CompareTo(Object obj)
        {
            ProcessItem otherItem = obj as ProcessItem;

            return process.ProcessName.CompareTo(otherItem.process.ProcessName);
        }
    }
}