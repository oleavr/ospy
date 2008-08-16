//
// Copyright (c) 2006-2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

//#define TESTING_PARSER

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
using oSpy.Util;
using System.ComponentModel;
using oSpy.SharpDumpLib;

namespace oSpy
{
    public partial class MainForm : Form
    {
        private Capture.Manager captureMgr = new Capture.Manager();
        private SoftwallForm swForm = new SoftwallForm();

        private Dump curDump = null;
        private ProgressForm curProgress = null;
        private string curOperation = null;

        public MainForm()
        {
            InitializeComponent();
        }

        private void NewOperation(string name)
        {
            curOperation = name;
            curProgress = new ProgressForm(curOperation);
        }

        private void curOperation_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            curProgress.ProgressUpdate(curOperation, e.ProgressPercentage);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCurrentDump();
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            saveMenuItem.Enabled = (curDump != null);
            saveUncompressedMenuItem.Enabled = (curDump != null);
            closeMenuItem.Enabled = (curDump != null);
        }

        private void newCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Capture.ChooseForm frm = new Capture.ChooseForm();

            System.Diagnostics.Process[] processes;
            oSpy.Capture.Device[] devices;
            bool restartDevices;
            if (!frm.GetSelection (out processes, out devices, out restartDevices))
                return;

            captureMgr.RestartDevices = restartDevices;

            if (processes.Length > 0 && devices.Length > 0)
            {
                MessageBox.Show ("Capturing from both processes and devices simultaneously is not yet supported.",
                                 "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            NewOperation("Starting capture");
            captureMgr.StartCapture(processes, swForm.GetRules(), devices, curProgress);

            if (curProgress.ShowDialog(this) != DialogResult.OK)
            {
                MessageBox.Show(String.Format("Failed to start capture: {0}", curProgress.GetOperationErrorMessage()),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Capture.ProgressForm capProgFrm = new Capture.ProgressForm(captureMgr);
            capProgFrm.ShowDialog();

            NewOperation("Stopping capture");
            captureMgr.StopCapture(curProgress);

            if (curProgress.ShowDialog(this) != DialogResult.OK)
            {
                MessageBox.Show(String.Format("Failed to stop capture: {0}", curProgress.GetOperationErrorMessage()),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            NewOperation("Processing capture");
            curProgress = new ProgressForm(curOperation);
            dumpBuilder.BuildAsync(captureMgr.CapturePath, captureMgr.EventCount, curOperation);

            curProgress.ShowDialog(this);
        }

        private void dumpBuilder_BuildCompleted(object sender, BuildCompletedEventArgs e)
        {
            captureMgr.CloseCapture();

            curProgress.OperationComplete();

            Dump dump = null;

            try
            {
                dump = e.Dump;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to process capture: {0}\n{1}", ex.InnerException.Message, ex.InnerException.StackTrace),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenDump(dump);
        }

        private byte[] uncompressedFileMagic = { (byte) 'o', (byte) 'S', (byte) 'p', (byte) 'y' };

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                NewOperation("Loading");

                Stream stream = File.OpenRead (openFileDialog.FileName);
                byte[] magic = new byte[4];
                stream.Read (magic, 0, magic.Length);

                // FIXME: not too elegant
                if (magic[0] != uncompressedFileMagic[0] ||
                    magic[1] != uncompressedFileMagic[1] ||
                    magic[2] != uncompressedFileMagic[2] ||
                    magic[3] != uncompressedFileMagic[3])
                {
                    stream.Seek (0, SeekOrigin.Begin);
                    stream = new BZip2InputStream (stream);
                }

                dumpLoader.LoadAsync(stream, curOperation);
                curProgress.ShowDialog(this);
            }
        }

        private void dumpLoader_LoadCompleted(object sender, LoadCompletedEventArgs e)
        {
            curProgress.OperationComplete();

            Dump dump = null;

            try
            {
                dump = e.Dump;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to load capture: {0}", ex.Message),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenDump(dump);
        }

        private void OpenDump(Dump dump)
        {
            CloseCurrentDump();

            curDump = dump;
            dataGridView.DataSource = null;

            NewOperation("Opening");
            Thread th = new Thread(new ThreadStart(DoOpenDump));
            th.Start();

            if (curProgress.ShowDialog(this) != DialogResult.OK)
            {
                MessageBox.Show(String.Format("Failed to open capture: {0}", curProgress.GetOperationErrorMessage()),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            playgroundToolStripMenuItem.Enabled = true;
#if TESTING_PARSER
            DumpParser parser = new DumpParser();
            parser.ParseProgressChanged += new ParseProgressChangedEventHandler(parser_ParseProgressChanged);
            parser.ParseCompleted += new ParseCompletedEventHandler(parser_ParseCompleted);

            NewOperation("Parsing");
            parser.ParseAsync(curDump, curOperation);
            curProgress.ShowDialog(this);
#endif
        }

        private void CloseCurrentDump()
        {
            playgroundToolStripMenuItem.Enabled = false;

            if (curDump == null)
                return;

            curDump.Close();
            curDump = null;

            dataSet.Tables[0].Clear();
            richTextBox.Clear();
        }

        private void DoOpenDump()
        {
            int n = 0;
            int count = curDump.Events.Count;

            try
            {
                int prevPct = -1;

                DataTable tbl = dataSet.Tables[0];
                tbl.BeginLoadData();
                DataRowCollection rows = tbl.Rows;

                foreach (Event ev in curDump.Events.Values)
                {
                    int pct = (int)(((float)n / (float)count) * 100.0f);
                    if (pct != prevPct)
                    {
                        prevPct = pct;
                        curProgress.ProgressUpdate(curOperation, pct);
                    }

                    rows.Add(ev.Id, ev.Timestamp, ev.ThreadId, CreateEventDescription (ev), ev);
                }

                tbl.EndLoadData();
            }
            catch (Exception e)
            {
                Invoke (new ThreadStart (CloseCurrentDump));
                curProgress.OperationFailed(e.Message);
                return;
            }

            Invoke(new ThreadStart(RestoreDataSource));

            curProgress.OperationComplete();
        }

        private string CreateEventDescription (Event ev)
        {
            switch (ev.Type)
            {
                case DumpEventType.FunctionCall:
                    string fullName = ev.Data.SelectSingleNode ("/event/name").InnerText;
                    string[] tokens = fullName.Split (new string[] { "::" }, 2, StringSplitOptions.None);
                    string shortName = tokens[tokens.Length - 1];

                    List<string> argList = new List<string> ();
                    foreach (XmlNode node in ev.Data.SelectNodes ("/event/arguments[@direction='in']/argument/value"))
                    {
                        if (node.Attributes["value"] != null)
                            argList.Add (node.Attributes["value"].Value);
                        else
                            argList.Add ("<FIXME>");
                    }

                    string retVal = ev.Data.SelectSingleNode ("/event/returnValue/value").Attributes["value"].Value;

                    return String.Format ("{0}({1}) => {2}", shortName, String.Join (", ", argList.ToArray ()), retVal);

                case DumpEventType.AsyncResult:
                    uint requestEventId = Convert.ToUInt32 (ev.Data.SelectSingleNode ("/event/requestId").InnerText);
                    return String.Format ("AsyncResult for event {0}", requestEventId);
                /* TODO: extend */
            }

            return "";
        }

        private void RestoreDataSource()
        {
            dataGridView.DataSource = bindingSource;
        }

#if TESTING_PARSER
        private Resource latestResource = null;

        private void parser_ParseProgressChanged(object sender, ParseProgressChangedEventArgs e)
        {
            string msg = curOperation;

            if (e.LatestResource != null)
            {
                latestResource = e.LatestResource;
            }

            if (latestResource != null)
            {
                msg = String.Format("{0}: last resource had handle 0x{1:x}", curOperation, latestResource.Handle);
            }

            curProgress.ProgressUpdate(msg, e.ProgressPercentage);
        }

        private void parser_ParseCompleted(object sender, ParseCompletedEventArgs e)
        {
            curProgress.OperationComplete();

            List<Process> processes = null;

            try
            {
                processes = e.Processes;

                int resCount = 0;
                foreach (Process proc in processes)
                {
                    resCount += proc.Resources.Count;
                }

                MessageBox.Show(String.Format("Successfully parsed data from {0} process(es) with a total of {1} resource(s).", processes.Count, resCount),
                                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                foreach (Process proc in processes)
                {
                    proc.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to parse capture: {0}\n{1}", ex.InnerException.Message, ex.InnerException.StackTrace),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
#endif

        private void Save (bool compressed)
        {
            if (saveFileDialog.ShowDialog () == DialogResult.OK)
            {
                NewOperation ("Saving");

                Stream stream = File.Open (saveFileDialog.FileName, FileMode.Create);
                if (compressed)
                {
                    stream = new BZip2OutputStream (stream);
                }
                else
                {
                    stream.Write (uncompressedFileMagic, 0, uncompressedFileMagic.Length);
                }

                dumpSaver.SaveAsync (curDump, stream, curOperation);
                curProgress.ShowDialog (this);
            }
        }

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            Save (true);
        }

        private void saveuncompressedToolStripMenuItem_Click (object sender, EventArgs e)
        {
            Save (false);
        }

        private void dumpSaver_SaveCompleted(object sender, SaveCompletedEventArgs e)
        {
            curProgress.OperationComplete();

            try
            {
                // TODO: this stream should be closed even if it fails
                e.Stream.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to save capture: {0}", ex.Message),
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void closeMenuItem_Click(object sender, EventArgs e)
        {
            CloseCurrentDump();
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
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

            DataGridViewRow row = dataGridView.SelectedRows[0];
            if (row.Cells.Count <= 1)
                return;

            Event ev = dataGridView.SelectedRows[0].Cells[4].Value as Event;

            string prettyXml;
            XmlHighlighter highlighter = new XmlHighlighter(XmlHighlightColorScheme.DarkBlueScheme);
            XmlUtils.PrettyPrint(ev.RawData, out prettyXml, highlighter);

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

        private void playgroundToolStripMenuItem_Click (object sender, EventArgs e)
        {
            if (curDump == null)
                return;

            Playground.VisualizationForm frm = new oSpy.Playground.VisualizationForm (curDump);
            frm.Show (this);
        }
    }
}
