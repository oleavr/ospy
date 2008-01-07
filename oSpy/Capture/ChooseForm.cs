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

namespace oSpy.Capture
{
    public partial class ChooseForm : Form
    {
        private int checkCount = 0;

        private ProcessViewUpdater processViewUpdater;
        private UsbViewUpdater usbViewUpdater;

        private ManagementEventWatcher processStartWatcher = null;
        private ManagementEventWatcher processStopWatcher = null;

        public ChooseForm ()
        {
            InitializeComponent ();

            processView.ListViewItemSorter = new ProcessViewItemComparer ();
            usbDevView.ListViewItemSorter = new DeviceViewItemComparer ();

            processViewUpdater = new ProcessViewUpdater (processView);
            usbViewUpdater = new UsbViewUpdater (usbDevView);

            WqlEventQuery startQuery = new WqlEventQuery ();
            startQuery.EventClassName = "Win32_ProcessStartTrace";

            WqlEventQuery stopQuery = new WqlEventQuery ();
            stopQuery.EventClassName = "Win32_ProcessStopTrace";

            ManagementEventWatcher w = new ManagementEventWatcher (startQuery);
            w.EventArrived += new EventArrivedEventHandler (ProcessEventArrived);
            w.Start();
            processStartWatcher = w;

            w = new ManagementEventWatcher (stopQuery);
            w.EventArrived += new EventArrivedEventHandler (ProcessEventArrived);
            w.Start ();
            processStopWatcher = w;
        }

        private void ProcessEventArrived (object o, EventArrivedEventArgs e)
        {
            processViewUpdater.Update ();
        }

        private void InjectForm_Shown (object sender, EventArgs e)
        {
            processViewUpdater.Update ();
            usbViewUpdater.Update ();

            UpdateButtons ();
        }

        private void ChooseForm_FormClosing (object sender, FormClosingEventArgs e)
        {
            processStartWatcher.Stop ();
            processStopWatcher.Stop ();

            processViewUpdater.BeginClose ();
            usbViewUpdater.BeginClose ();

            while (!processViewUpdater.IsClosed || !usbViewUpdater.IsClosed)
            {
                Application.DoEvents ();
                Thread.Sleep (10);
            }
        }

        protected override void WndProc (ref Message m)
        {
            base.WndProc (ref m);

            if (m.Msg == WinApi.WM_DEVICECHANGE)
                usbViewUpdater.Update ();
        }

        private void UpdateButtons ()
        {
            startBtn.Enabled = (checkCount > 0);
        }

        private void anyView_ItemCheck (object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                checkCount++;
            else
                checkCount--;

            UpdateButtons ();
        }

        private void startBtn_Click (object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        public bool GetSelection (out Process[] processes, out Device[] devices, out bool restartDevices)
        {
            List<Process> procList = new List<Process> ();
            List<Device> devList = new List<Device> ();

            if (ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in processView.Items)
                {
                    if (item.Checked)
                        procList.Add (item.Tag as Process);
                }

                foreach (ListViewItem item in usbDevView.Items)
                {
                    if (item.Checked)
                        devList.Add (item.Tag as Device);
                }
            }

            processes = procList.ToArray ();
            devices = devList.ToArray ();
            restartDevices = restartDevicesCheckBox.Checked;

            return (processes.Length > 0 || devices.Length > 0);
        }
    }

    public class ProcessViewItemComparer : System.Collections.IComparer
    {
        public int Compare (object itemA, object itemB)
        {
            Process a = (itemA as ListViewItem).Tag as Process;
            Process b = (itemB as ListViewItem).Tag as Process;

            if (a == b)
                return 0;
            else
                return a.ProcessName.CompareTo (b.ProcessName);
        }
    }

    public class DeviceViewItemComparer : System.Collections.IComparer
    {
        public int Compare (object itemA, object itemB)
        {
            Device a = (itemA as ListViewItem).Tag as Device;
            Device b = (itemB as ListViewItem).Tag as Device;

            if (a == b)
                return 0;
            else if (b.Present != a.Present)
                return b.Present.CompareTo (a.Present);
            else
                return a.Name.CompareTo (b.Name);
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

        private delegate object[] CreateItemsDelegate ();
        private delegate void ApplyItemsDelegate (object[] items);

        public ListViewUpdater (ListView view)
        {
            this.view = view;
            imageList = new ImageList ();
            view.SmallImageList = imageList;

            workerThread = new Thread (WorkerThreadFunc);
            stopEvent = new EventWaitHandle (false, EventResetMode.ManualReset);
            workEvent = new EventWaitHandle (false, EventResetMode.ManualReset);
            workerThread.Start ();
        }

        public void Close ()
        {
            BeginClose ();

            while (!workerThread.Join (50))
            {
                Application.DoEvents ();
            }
        }

        public void BeginClose ()
        {
            stopEvent.Set ();
        }

        public bool IsClosed
        {
            get { return !workerThread.IsAlive; }
        }

        public void Update ()
        {
            workEvent.Set ();
        }

        private void WorkerThreadFunc ()
        {
            WaitHandle[] handles = { this.stopEvent, this.workEvent };

            int index = 0;

            do
            {
                index = EventWaitHandle.WaitAny (handles, Timeout.Infinite, false);
                if (index == 1)
                {
                    workEvent.Reset ();

                    if (hasUpdated)
                    {
                        index = EventWaitHandle.WaitAny (handles, 500, false);
                        if (index == WaitHandle.WaitTimeout)
                        {
                            DoUpdate ();
                        }
                    }
                    else
                    {
                        hasUpdated = true;
                        DoUpdate ();
                    }
                }
            }
            while (index != 0);
        }

        private void DoUpdate ()
        {
            object[] items = CreateItems ();

            if (stopEvent.WaitOne (0, false))
                return;

            view.Invoke (new ApplyItemsDelegate (ApplyItems), new object[] { items, });
        }

        protected abstract object[] CreateItems ();
        protected abstract object GetKeyFromItem (object item);
        protected abstract void UpdateListViewItem (ListViewItem viewItem, object item);

        private void ApplyItems (object[] items)
        {
            System.Collections.Hashtable oldItems = new System.Collections.Hashtable (items.Length);
            foreach (ListViewItem viewItem in view.Items)
            {
                object key = GetKeyFromItem (viewItem.Tag);
                oldItems[key] = viewItem;
            }

            foreach (object item in items)
            {
                object key = GetKeyFromItem (item);

                if (oldItems.ContainsKey (key))
                {
                    ListViewItem viewItem = oldItems[key] as ListViewItem;
                    UpdateListViewItem (viewItem, item);
                    oldItems.Remove (key);
                }
                else
                {
                    ListViewItem viewItem = new ListViewItem ();
                    UpdateListViewItem (viewItem, item);
                    view.Items.Add (viewItem);
                }
            }

            foreach (ListViewItem item in oldItems.Values)
                view.Items.Remove (item);

            view.Sort ();
        }
    }

    public class ProcessViewUpdater : ListViewUpdater
    {
        private Dictionary<Process, Icon> processIcons = new Dictionary<Process, Icon> ();

        public ProcessViewUpdater (ListView view)
            : base (view)
        {
        }

        protected override object[] CreateItems ()
        {
            int ourSessionId = Process.GetCurrentProcess ().SessionId;

            List<Process> result = new List<Process> ();
            foreach (Process process in Process.GetProcesses ())
            {
                if (process.SessionId == ourSessionId)
                {
                    result.Add (process);
                    processIcons[process] = GetProcessIcon (process);
                }
            }

            return result.ToArray ();
        }

        private Icon GetProcessIcon (Process proc)
        {
            IntPtr hwnd = proc.MainWindowHandle;

            IntPtr iconHandle = WinApi.SendMessage (hwnd, WinApi.WM_GETICON, WinApi.ICON_SMALL2, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.SendMessage (hwnd, WinApi.WM_GETICON, WinApi.ICON_SMALL, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.SendMessage (hwnd, WinApi.WM_GETICON, WinApi.ICON_BIG, 0);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.GetClassLongPtr (hwnd, WinApi.GCL_HICON);
            if (iconHandle == IntPtr.Zero)
                iconHandle = WinApi.GetClassLongPtr (hwnd, WinApi.GCL_HICONSM);

            Icon icon = null;

            if (iconHandle != IntPtr.Zero)
            {
                try { icon = Icon.FromHandle (iconHandle); }
                catch (Exception) { }
            }

            if (icon == null)
            {
                try { icon = Icon.ExtractAssociatedIcon (proc.MainModule.FileName); }
                catch (Exception) { }
            }

            return icon;
        }

        protected override object GetKeyFromItem (object item)
        {
            return (item as Process).Id;
        }

        protected override void UpdateListViewItem (ListViewItem viewItem, object item)
        {
            Process process = item as Process;

            viewItem.Name = String.Format ("{0} ({1})", process.ProcessName, process.Id);
            viewItem.Text = viewItem.Name;
            viewItem.Tag = process;

            Icon icon = processIcons[process];
            if (viewItem.ImageIndex >= 0)
            {
                if (icon != null)
                    imageList.Images[viewItem.ImageIndex] = icon.ToBitmap ();
            }
            else
            {
                if (icon != null)
                {
                    imageList.Images.Add (icon);

                    viewItem.ImageIndex = imageListIndex++;
                }
            }
        }
    }

    public class UsbViewUpdater : ListViewUpdater
    {
        private DeviceList devList = null;

        public UsbViewUpdater (ListView view)
            : base (view)
        {
        }

        protected override object[] CreateItems ()
        {
            if (devList != null)
                devList.Dispose ();

            devList = new DeviceList (DeviceEnumerator.USB);
            return devList.Devices.ToArray ();
        }

        protected override object GetKeyFromItem (object item)
        {
            return (item as Device).HardwareId;
        }

        protected override void UpdateListViewItem (ListViewItem viewItem, object item)
        {
            Device device = item as Device;

            viewItem.Name = device.Name;
            viewItem.Text = viewItem.Name;
            viewItem.Tag = device;

            viewItem.ForeColor = (device.Present) ? Color.Green : Color.Black;

            if (device.HasLowerFilter (Constants.UsbAgentName))
                viewItem.Checked = true;

            if (viewItem.ImageIndex >= 0)
            {
                imageList.Images[viewItem.ImageIndex] = device.SmallIcon.ToBitmap ();
            }
            else
            {
                Icon smallIcon = device.SmallIcon;
                Icon largeIcon = device.LargeIcon;

                if (smallIcon != null && largeIcon != null)
                {
                    imageList.Images.Add (smallIcon);

                    viewItem.ImageIndex = imageListIndex++;
                }
            }
        }
    }
}
