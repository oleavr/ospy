//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Management;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace oSpy.Capture
{
    public partial class AttachForm : Form
    {
        private int checkCount = 0;

        private ProcessViewUpdater processViewUpdater;

        private ManagementEventWatcher processStartWatcher = null;
        private ManagementEventWatcher processStopWatcher = null;

        public AttachForm()
        {
            InitializeComponent();

            processView.ListViewItemSorter = new ProcessViewItemComparer();

            processViewUpdater = new ProcessViewUpdater(processView);

            WqlEventQuery startQuery = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Win32_Process\"");
            ManagementEventWatcher w = new ManagementEventWatcher(startQuery);
            w.EventArrived += new EventArrivedEventHandler(ProcessEventArrived);
            w.Start();
            processStartWatcher = w;

            WqlEventQuery stopQuery = new WqlEventQuery("__InstanceDeletionEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Win32_Process\"");
            w = new ManagementEventWatcher(stopQuery);
            w.EventArrived += new EventArrivedEventHandler(ProcessEventArrived);
            w.Start();
            processStopWatcher = w;

            x64NoteLbl.Visible = EasyHook.RemoteHooking.IsX64System;
        }

        public AttachDetails GetDetails()
        {
            AttachDetails details = null;

            if (ShowDialog() == DialogResult.OK)
            {
                List<Process> procList = new List<Process>();

                foreach (ListViewItem item in processView.Items)
                {
                    if (item.Checked)
                        procList.Add(item.Tag as Process);
                }

                details = new AttachDetails(procList.ToArray());
            }

            return details;
        }

        private void ProcessEventArrived(object o, EventArrivedEventArgs e)
        {
            processViewUpdater.Update();
        }

        private void InjectForm_Shown(object sender, EventArgs e)
        {
            processViewUpdater.Update();

            UpdateButtons();
        }

        private void ChooseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            processStartWatcher.Stop();
            processStopWatcher.Stop();

            processViewUpdater.BeginClose();

            while (!processViewUpdater.IsClosed)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }

        private void UpdateButtons()
        {
            startBtn.Enabled = (checkCount > 0);
        }

        private void anyView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
                e.Item.Checked = !e.Item.Checked;
        }

        private void anyView_ItemCheck(object sender, ItemCheckEventArgs e)
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
    }

    public class ProcessViewItemComparer : System.Collections.IComparer
    {
        private int curSessionId;

        public ProcessViewItemComparer()
        {
            curSessionId = Process.GetCurrentProcess().SessionId;
        }

        public int Compare(object itemA, object itemB)
        {
            Process a = (itemA as ListViewItem).Tag as Process;
            Process b = (itemB as ListViewItem).Tag as Process;

            if (a.SessionId != b.SessionId && ((a.SessionId == curSessionId) || (b.SessionId == curSessionId)))
            {
                if (a.SessionId == curSessionId)
                    return -1;
                else
                    return 1;
            }
            else
            {
                if (a == b)
                    return 0;
                else
                    return a.ProcessName.CompareTo(b.ProcessName);
            }
        }
    }

    public abstract class ListViewUpdater
    {
        protected ListView view;
        protected ImageList imageList;
        protected int imageListIndex = 0;
        private Thread workerThread;
        private EventWaitHandle stopEvent;
        private EventWaitHandle workEvent;
        private bool hasUpdated = false;

        private delegate object[] CreateItemsDelegate();
        private delegate void ApplyItemsDelegate(object[] items);

        public ListViewUpdater(ListView view)
        {
            this.view = view;
            imageList = new ImageList();
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            view.SmallImageList = imageList;

            workerThread = new Thread(WorkerThreadFunc);
            stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            workEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
            workerThread.Start();
        }

        public void Close()
        {
            BeginClose();

            while (!workerThread.Join(50))
            {
                Application.DoEvents();
            }
        }

        public void BeginClose()
        {
            stopEvent.Set();
        }

        public bool IsClosed
        {
            get { return !workerThread.IsAlive; }
        }

        public void Update()
        {
            workEvent.Set();
        }

        private void WorkerThreadFunc()
        {
            WaitHandle[] handles = { this.stopEvent, this.workEvent };

            int index = 0;

            do
            {
                index = EventWaitHandle.WaitAny(handles, Timeout.Infinite, false);
                if (index == 1)
                {
                    workEvent.Reset();

                    if (hasUpdated)
                    {
                        index = EventWaitHandle.WaitAny(handles, 500, false);
                        if (index == WaitHandle.WaitTimeout)
                        {
                            DoUpdate();
                        }
                    }
                    else
                    {
                        hasUpdated = true;
                        DoUpdate();
                    }
                }
            }
            while (index != 0);
        }

        private void DoUpdate()
        {
            object[] items = CreateItems();

            if (stopEvent.WaitOne(0, false))
                return;

            view.Invoke(new ApplyItemsDelegate(ApplyItems), new object[] { items, });
        }

        protected abstract object[] CreateItems();
        protected abstract object GetKeyFromItem(object item);
        protected abstract void UpdateListViewItem(ListViewItem viewItem, object item);

        private void ApplyItems(object[] items)
        {
            view.BeginUpdate();

            try
            {
                System.Collections.Hashtable oldItems = new System.Collections.Hashtable(items.Length);
                foreach (ListViewItem viewItem in view.Items)
                {
                    object key = GetKeyFromItem(viewItem.Tag);
                    oldItems[key] = viewItem;
                }

                foreach (object item in items)
                {
                    object key = GetKeyFromItem(item);

                    if (oldItems.ContainsKey(key))
                    {
                        ListViewItem viewItem = oldItems[key] as ListViewItem;
                        UpdateListViewItem(viewItem, item);
                        oldItems.Remove(key);
                    }
                    else
                    {
                        ListViewItem viewItem = new ListViewItem();
                        UpdateListViewItem(viewItem, item);
                        view.Items.Add(viewItem);
                    }
                }

                foreach (ListViewItem item in oldItems.Values)
                    view.Items.Remove(item);

                view.Sort();
            }
            finally
            {
                view.EndUpdate();
            }
        }
    }

    public class ProcessViewUpdater : ListViewUpdater
    {
        private Dictionary<Process, Icon> processIcons = new Dictionary<Process, Icon>();

        public ProcessViewUpdater(ListView view)
            : base(view)
        {
        }

        protected override object[] CreateItems()
        {
            Process curProcess = Process.GetCurrentProcess();

            List<Process> result = new List<Process>();
            foreach (Process process in Process.GetProcesses())
            {
                // Skip ourself
                if (process.Id == curProcess.Id)
                    continue;

                // Skip special PIDs
                if (process.Id == 0 || process.Id == 4)
                    continue;

                // Skip EasyHook services
                string processNameLower = process.ProcessName.ToLower();
                if (processNameLower == "easyhook32svc" || processNameLower == "easyhook64svc")
                    continue;
                // And any oSpy instances
                if (processNameLower == "ospy" || processNameLower == "ospy.vshost")
                    continue;

                // And also 64 bit processes on x64
                if (EasyHook.RemoteHooking.IsX64System)
                {
                    SafeHandle processHandle = new SafeFileHandle(WinApi.OpenProcess(WinApi.PROCESS_QUERY_INFORMATION, false, (uint) process.Id), true);
                    if (!processHandle.IsInvalid)
                    {
                        bool processIs32Bit = false;
                        if (WinApi.IsWow64Process(processHandle.DangerousGetHandle(), out processIs32Bit) && !processIs32Bit)
                            continue;
                    }
                }

                result.Add(process);
                processIcons[process] = GetProcessIcon(process);
            }

            return result.ToArray();
        }

        private Icon GetProcessIcon(Process proc)
        {
            IntPtr hwnd = proc.MainWindowHandle;

            IntPtr iconHandle = WinApi.SendMessage(hwnd, WinApi.WM_GETICON, WinApi.ICON_SMALL2, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.SendMessage(hwnd, WinApi.WM_GETICON, WinApi.ICON_SMALL, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.SendMessage(hwnd, WinApi.WM_GETICON, WinApi.ICON_BIG, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.GetClassLongPtr(hwnd, WinApi.GCL_HICON);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.GetClassLongPtr(hwnd, WinApi.GCL_HICONSM);

            Icon icon = null;

            if (iconHandle != IntPtr.Zero)
            {
                try { icon = Icon.FromHandle(iconHandle); }
                catch (Exception) { }
            }

            if (icon == null)
            {
                try { icon = Icon.ExtractAssociatedIcon(proc.MainModule.FileName); }
                catch (Exception) { }
            }

            return icon;
        }

        protected override object GetKeyFromItem(object item)
        {
            return (item as Process).Id;
        }

        protected override void UpdateListViewItem(ListViewItem viewItem, object item)
        {
            Process process = item as Process;

            viewItem.Name = String.Format("{0} ({1})", process.ProcessName, process.Id);
            viewItem.Text = viewItem.Name;
            viewItem.Tag = process;

            Icon icon = processIcons[process];
            if (viewItem.ImageIndex < 0)
            {
                if (icon != null)
                {
                    imageList.Images.Add(icon);

                    viewItem.ImageIndex = imageListIndex++;
                }
            }
        }
    }
}
