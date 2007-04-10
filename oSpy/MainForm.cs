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
using System.Threading;
using System.Xml;
using System.Diagnostics;
using oSpy.Util;

namespace oSpy
{
    public partial class MainForm : Form
    {
        private Capture.Manager captureMgr;
        private SoftwallForm swForm;

        private Capture.DumpFile curDump;
        private ProgressForm curProgress;

        public MainForm()
        {
            InitializeComponent();

            captureMgr = new Capture.Manager();
            swForm = new SoftwallForm();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeMenuItem.PerformClick();
        }

        private void newCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Capture.ChooseForm frm = new Capture.ChooseForm();

            Process[] processes = frm.GetSelectedProcesses();
            if (processes.Length == 0)
                return;

            curProgress = new ProgressForm("Starting capture");

            try
            {
                captureMgr.StartCapture(processes, curProgress);

                if (curProgress.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(String.Format("Failed to start capture: {0}", curProgress.GetOperationErrorMessage()),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Capture.ProgressForm capProgFrm = new Capture.ProgressForm(captureMgr);
                capProgFrm.ShowDialog();

                curProgress = new ProgressForm("Stopping capture");

                captureMgr.StopCapture(curProgress);

                if (curProgress.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(String.Format("Failed to stop capture: {0}", curProgress.GetOperationErrorMessage()),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                curDump = captureMgr.CaptureResult;
                curProgress = new ProgressForm("Opening");

                Thread th = new Thread(new ParameterizedThreadStart(DoOpenDump));
                th.Start(null);

                if (curProgress.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(String.Format("Failed to open capture: {0}", curProgress.GetOperationErrorMessage()),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            finally
            {
                curProgress = null;
            }
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                closeMenuItem.PerformClick();

                curDump = new Capture.DumpFile();
                curProgress = new ProgressForm("Opening");

                Thread th = new Thread(new ParameterizedThreadStart(DoOpenDump));
                th.Start(openFileDialog.FileName);

                if (curProgress.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(String.Format("Failed to open capture: {0}", curProgress.GetOperationErrorMessage()),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                curProgress = null;
            }
        }

        private void DoOpenDump(object obj)
        {
            string filename = obj as string;

            try
            {
                curDump.Load(filename, curProgress);
            }
            catch (Exception e)
            {
                curDump = null;
                curProgress.OperationFailed(e.Message);
                return;
            }

            Invoke(new ThreadStart(RestoreDataSource));

            curProgress.OperationComplete();
        }

        private void RestoreDataSource()
        {
            dataGridView.DataSource = curDump.Events;
            dataGridView.DataMember = "events";
        }

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                curProgress = new ProgressForm("Saving");

                Thread th = new Thread(new ParameterizedThreadStart(DoSaveDump));
                th.Start(saveFileDialog.FileName);

                if (curProgress.ShowDialog(this) != DialogResult.OK)
                {
                    MessageBox.Show(String.Format("Failed to save capture: {0}", curProgress.GetOperationErrorMessage()),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                curProgress = null;
            }
        }

        private void DoSaveDump(object obj)
        {
            string filename = obj as string;

            try
            {
                curDump.Save(filename, curProgress);
            }
            catch (Exception e)
            {
                curProgress.OperationFailed(e.Message);
                return;
            }

            curProgress.OperationComplete();
        }

        private void closeMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView.DataSource = null;

            if (curDump != null)
            {
                curDump.Close();
                curDump = null;
            }

            richTextBox.Clear();
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            if (curDump != null)
            {
                curDump.Close();
            }

            Close();
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            richTextBox.Clear();
            if (dataGridView.SelectedRows.Count < 1)
                return;

            string evData = curDump.ExtractEventData((uint) dataGridView.SelectedRows[0].Cells[0].Value);

            string prettyXml;
            XmlHighlighter highlighter = new XmlHighlighter(XmlHighlightColorScheme.DarkBlueScheme);
            XmlUtils.PrettyPrint(evData, out prettyXml, highlighter);

            richTextBox.Text = prettyXml;
            highlighter.HighlightRichTextBox(richTextBox);
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

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            saveMenuItem.Enabled = (curDump != null);
            closeMenuItem.Enabled = (curDump != null);
        }
    }
}
