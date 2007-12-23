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

namespace oSpy.Capture
{
    public partial class ChooseForm : Form
    {
        private int checkCount = 0;

        public ChooseForm ()
        {
            InitializeComponent ();
        }

        private void InjectForm_Shown (object sender, EventArgs e)
        {
            UpdateProcessList ();
            UpdateUsbDeviceList ();
            UpdateButtons ();
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

        private void UpdateUsbDeviceList ()
        {
            usbDevView.Items.Clear ();
            
            usbImagesSmall.Images.Clear ();
            usbImagesSmall.ImageSize = new Size (16, 16);

            usbImagesLarge.Images.Clear ();

            int imageIndex = 0;

            DeviceList devList = new DeviceList (DeviceEnumerator.USB);
            foreach (Device device in devList.Devices)
            {
                ListViewItem item = new ListViewItem (device.Name);

                if (device.Present)
                    item.ForeColor = Color.Green;

                Icon smallIcon = device.SmallIcon;
                Icon largeIcon = device.LargeIcon;
                if (smallIcon != null && largeIcon != null)
                {
                    usbImagesSmall.Images.Add (smallIcon);
                    usbImagesLarge.Images.Add (largeIcon);

                    item.ImageIndex = imageIndex++;
                }

                usbDevView.Items.Add (item);
            }
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

        public Process[] GetSelectedProcesses()
        {
            List<Process> result = new List<Process>();

            if (ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < processList.Items.Count; i++)
                {
                    if (processList.GetItemChecked(i))
                    {
                        result.Add((processList.Items[i] as ProcessItem).Process);
                    }
                }
            }

            return result.ToArray();
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
}
