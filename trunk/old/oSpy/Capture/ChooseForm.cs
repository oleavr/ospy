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

namespace oSpy.Capture
{
    public partial class ChooseForm : Form
    {
        private int checkCount = 0;

        public ChooseForm()
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