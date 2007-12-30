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
using System.Threading;
using System.Runtime.InteropServices;
using System.Management;

namespace oSpy.Capture
{
    public partial class ChooseForm : Form
    {
        private int checkCount = 0;

        private delegate void AsyncMethodCaller ();
        private List<AsyncMethodCaller> pendingOperations = new List<AsyncMethodCaller> ();

        private DeviceList usbDevList = null;
        private int usbViewImageIndex = 0;

        private ManagementEventWatcher processStartWatcher = null;
        private ManagementEventWatcher processStopWatcher = null;

        public ChooseForm ()
        {
            InitializeComponent ();

            usbDevView.ListViewItemSorter = new DeviceViewItemComparer ();

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
            if (processList.InvokeRequired)
            {
                Invoke (new EventArrivedEventHandler (ProcessEventArrived), o, e);
                return;
            }

            UpdateProcessList ();
        }

        private void InjectForm_Shown (object sender, EventArgs e)
        {
            UpdateProcessList ();
            BeginUpdatingUsbDeviceList ();
            UpdateButtons ();
        }

        private void ChooseForm_FormClosing (object sender, FormClosingEventArgs e)
        {
            processStartWatcher.Stop ();
            processStopWatcher.Stop ();

            WaitForPendingOperations ();
        }

        private void WaitForPendingOperations ()
        {
            while (pendingOperations.Count > 0)
            {
                Thread.Sleep (10);
                Application.DoEvents ();
            }
        }

        protected override void WndProc (ref Message m)
        {
            base.WndProc (ref m);

            if (m.Msg == WinApi.WM_DEVICECHANGE)
            {
                updateHwListTimer.Enabled = false;
                updateHwListTimer.Enabled = true;
            }
        }

        private void UpdateProcessList ()
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

        private void updateHwListTimer_Tick (object sender, EventArgs e)
        {
            updateHwListTimer.Enabled = false;

            WaitForPendingOperations ();
            BeginUpdatingUsbDeviceList ();
        }

        private void BeginUpdatingUsbDeviceList ()
        {
            AsyncMethodCaller caller = new AsyncMethodCaller (CreateUsbDeviceList);
            caller.BeginInvoke (new AsyncCallback (UsbDeviceListCreated), caller);
            pendingOperations.Add (caller);
        }

        private void CreateUsbDeviceList ()
        {
            if (usbDevList != null)
                usbDevList.Dispose ();
            usbDevList = new DeviceList (DeviceEnumerator.USB);
        }

        private void UsbDeviceListCreated (IAsyncResult ar)
        {
            AsyncMethodCaller caller = (AsyncMethodCaller)ar.AsyncState;
            caller.EndInvoke (ar);

            if (usbDevView.InvokeRequired)
                Invoke (new AsyncMethodCaller (ApplyUsbDeviceList));
            else
                ApplyUsbDeviceList ();

            pendingOperations.Remove (caller);
        }

        private void ApplyUsbDeviceList ()
        {
            bool sortNeeded = false;

            if (usbDevView.Items.Count == 0)
                usbImagesSmall.ImageSize = new Size (16, 16);

            Dictionary<string, ListViewItem> oldItems = new Dictionary<string, ListViewItem> ();
            foreach (ListViewItem item in usbDevView.Items)
            {
                Device device = item.Tag as Device;
                oldItems[device.HardwareId] = item;
            }

            foreach (Device device in usbDevList.Devices)
            {
                if (oldItems.ContainsKey (device.HardwareId))
                {
                    ListViewItem item = oldItems[device.HardwareId];

                    Device oldDevice = item.Tag as Device;
                    if (device.Name != oldDevice.Name ||
                        device.Present != oldDevice.Present)
                    {
                        sortNeeded = true;
                    }

                    UpdateListViewItemWithDevice (item, device);

                    oldItems.Remove (device.HardwareId);
                }
                else
                {
                    ListViewItem item = new ListViewItem (device.Name);
                    UpdateListViewItemWithDevice (item, device);
                    usbDevView.Items.Add (item);

                    sortNeeded = true;
                }
            }

            foreach (ListViewItem item in oldItems.Values)
                usbDevView.Items.Remove (item);

            if (sortNeeded)
                usbDevView.Sort ();
        }

        private void UpdateListViewItemWithDevice (ListViewItem item, Device device)
        {
            item.Name = device.Name;
            item.Tag = device;

            item.ForeColor = (device.Present) ? Color.Green : Color.Black;

            if (device.HasLowerFilter (Constants.UsbAgentName))
                item.Checked = true;

            if (item.ImageIndex >= 0)
            {
                usbImagesSmall.Images[item.ImageIndex] = device.SmallIcon.ToBitmap ();
                usbImagesLarge.Images[item.ImageIndex] = device.LargeIcon.ToBitmap ();
            }
            else
            {
                Icon smallIcon = device.SmallIcon;
                Icon largeIcon = device.LargeIcon;

                if (smallIcon != null && largeIcon != null)
                {
                    usbImagesSmall.Images.Add (smallIcon);
                    usbImagesLarge.Images.Add (largeIcon);

                    item.ImageIndex = usbViewImageIndex++;
                }
            }
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

        public bool GetSelection (out Process[] processes, out Device[] devices)
        {
            List<Process> procList = new List<Process> ();
            List<Device> devList = new List<Device> ();

            if (ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < processList.Items.Count; i++)
                {
                    if (processList.GetItemChecked(i))
                    {
                        procList.Add((processList.Items[i] as ProcessItem).Process);
                    }
                }

                foreach (ListViewItem item in usbDevView.Items)
                {
                    if (item.Checked)
                        devList.Add (item.Tag as Device);
                }
            }

            processes = procList.ToArray ();
            devices = devList.ToArray ();

            return (processes.Length > 0 || devices.Length > 0);
        }
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
}
