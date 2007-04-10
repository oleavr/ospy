//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

using System.Windows.Forms;
using System.Data;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;
using System.Text.RegularExpressions;
using oSpy.Capture;
using System.Threading;
using System.Xml;
using System.Diagnostics;

namespace oSpy
{
    public partial class MainForm : Form
    {
        private Capture.Manager captureMgr;
        private SoftwallForm swForm;

        public MainForm()
        {
            InitializeComponent();

            captureMgr = new Capture.Manager();
            swForm = new SoftwallForm();

            ClearState();
        }

        protected void ClearState()
        {
            dataSet.Clear();
            richTextBox.Clear();
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ProgressForm progFrm = new ProgressForm("Opening");

                ClearState();

                dataGridView.DataSource = null;
                dataSet.Tables[0].BeginLoadData();

                clearMenuItem.PerformClick();

                Thread th = new Thread(new ParameterizedThreadStart(OpenFile));
                th.Start(progFrm);

                progFrm.ShowDialog(this);
            }
        }

        private void OpenFile(object param)
        {
            IProgressFeedback progress = param as IProgressFeedback;

            Capture.DumpFile df = new Capture.DumpFile();
            df.Load(openFileDialog.FileName, progress);

            foreach (DumpEvent ev in df.Events.Values)
            {
                DataRow row = dataSet.Tables[0].NewRow();
                row[eventCol] = ev;
                row.AcceptChanges();
                dataSet.Tables[0].Rows.Add(row);
            }

            dataSet.Tables[0].EndLoadData();

            progress.ProgressUpdate("Opened", 100);

            progress.ProgressUpdate("Finishing", 100);
            Invoke(new ThreadStart(RestoreDataSource));

            progress.OperationComplete();
        }

        private void RestoreDataSource()
        {
            dataGridView.DataSource = bindingSource;
        }

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create);
                BZip2OutputStream stream = new BZip2OutputStream(fs);
                dataSet.WriteXml(stream);
                stream.Close();
                fs.Close();
            }
        }

        private void newCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Capture.ChooseForm frm = new Capture.ChooseForm();

            Process[] processes = frm.GetSelectedProcesses();
            if (processes.Length == 0)
                return;

            ProgressForm progFrm = new ProgressForm("Starting capture");

            captureMgr.StartCapture(processes, progFrm);

            if (progFrm.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(String.Format("Failed to start capture: {0}", progFrm.GetOperationErrorMessage()),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Capture.ProgressForm capProgFrm = new Capture.ProgressForm(captureMgr);
            capProgFrm.ShowDialog();

            progFrm = new ProgressForm("Stopping capture");

            captureMgr.StopCapture(progFrm);

            if (progFrm.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show(String.Format("Failed to stop capture: {0}", progFrm.GetOperationErrorMessage()),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                captureMgr.SaveCapture(saveFileDialog.FileName);
            }
            else
            {
                captureMgr.DiscardCapture();
            }
        }

        private void dataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            DataGridViewRow row = dataGridView.Rows[e.RowIndex];
            DumpEvent ev = row.Cells[eventTextCol.Index].Value as DumpEvent;

            switch (e.ColumnIndex)
            {
                case 1:
                    e.Value = ev.Id;
                    break;
                case 2:
                    e.Value = ev.Timestamp;
                    break;
                case 3:
                    e.Value = ev.Type.ToString();
                    break;
            }
        }

        private void clearMenuItem_Click(object sender, EventArgs e)
        {
            ClearState();
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                DumpEvent ev = row.Cells[eventTextCol.Index].Value as DumpEvent;
                builder.Append(ev.Data);
                builder.Append("\n\n");
            }

            richTextBox.Text = builder.ToString();
        }

        private void manageSoftwallRulesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            swForm.ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBoxForm frm = new AboutBoxForm();
            frm.ShowDialog();
        }
    }
}
