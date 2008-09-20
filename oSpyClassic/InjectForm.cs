/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

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

namespace oSpyClassic
{
    public partial class InjectForm : Form
    {
        public InjectForm()
        {
            InitializeComponent();
        }

        private void InjectForm_Shown(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void RefreshList()
        {
            listView.Clear();
            foreach (Process proc in Process.GetProcesses())
            {
                string name;

                name = String.Format("{0} ({1})", proc.ProcessName, proc.Id);

                ListViewItem item = new ListViewItem(name);
                item.Tag = proc;

                listView.Items.Add(item);
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void killButton_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem item in listView.SelectedItems)
            {
                Process proc = item.Tag as Process;
                proc.Kill();
            }

            Thread.Sleep(1000);

            RefreshList();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void asRestartButton_Click(object sender, EventArgs e)
        {
            int count = 0;

            RefreshList();

            foreach (ListViewItem item in listView.Items)
            {
                Process proc = item.Tag as Process;

                string name = proc.ProcessName.ToLower();

                if (name == "rapimgr" ||
                    name == "wcescomm" ||
                    name == "wcesmgr")
                {
                    count++;
                    proc.Kill();
                }
            }

            if (count > 0)
            {
                Thread.Sleep(1000);
            }

            RefreshList();
        }
        
        private void asInjectButton_Click(object sender, EventArgs e)
        {            
            int count = 0;

            RefreshList();

            foreach (ListViewItem item in listView.Items)
            {
                Process proc = item.Tag as Process;

                string name = proc.ProcessName.ToLower();

                if (name == "rapimgr" ||
                    name == "wcescomm" ||
                    name == "wcesmgr")
                {
                    count++;
                    if (!InjectDLL(proc.Id))
                    {
                        MessageBox.Show(this, "Failed to inject agent into '" + name + "'.", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            if (count > 0)
            {
                MessageBox.Show(this, String.Format("Agent successfully injected into {0} processes.", count),
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

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
            if (listView.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem item in listView.SelectedItems)
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
    }
}