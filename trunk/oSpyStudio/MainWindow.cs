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
using System.IO;
using System.ComponentModel;
using oSpy.SharpDumpLib;
using ICSharpCode.SharpZipLib.BZip2;

namespace oSpyStudio
{
    public class MainWindow
    {
        #region Fields - UI

        [Glade.Widget("mainWindow")]
        private Gtk.Window window = null;

        [Glade.Widget]
        private Gtk.TreeView processView = null;
        private Gtk.ListStore processModel = new Gtk.ListStore (typeof (Process));

        [Glade.Widget]
        private Gtk.TreeView resourceView = null;
        private Gtk.ListStore resourceModel = new Gtk.ListStore (typeof (Resource));

        [Glade.Widget]
        private Gtk.TreeView transferView = null;
        private Gtk.ListStore transferModel = new Gtk.ListStore (typeof (DataTransfer));

        [Glade.Widget]
        private Gtk.VPaned mainVPaned = null;

        private Widgets.DataView chunkView = new Widgets.DataView ();
        private Gtk.ListStore chunkModel = new Gtk.ListStore (typeof (byte []), typeof (string), typeof (string));

        [Glade.Widget]
        private Gtk.Statusbar statusbar = null;

        private Gdk.Pixbuf incomingPixbuf;
        private Gdk.Pixbuf outgoingPixbuf;
        private Gdk.Pixbuf socketPixbuf;
        private Gdk.Pixbuf securePixbuf;

        private Gtk.Button cancelButton = new Gtk.Button ();
        private Gtk.ProgressBar progressbar = new Gtk.ProgressBar ();

        #endregion // Fields - Glade

        #region Fields - task-related

        private Dump curDump;
        private UITask curTask;
        private List<Process> curProcesses;
        private List<Process> selectedProcesses = new List<Process> ();

        private DumpLoader dumpLoader = new DumpLoader ();
        private DumpParser dumpParser = new DumpParser ();

        #endregion // Fields - task-related

        #region Construction

        public MainWindow ()
        {
            //Glade.XML xml = new Glade.XML (new System.IO.MemoryStream (oSpyStudio.Properties.Resources.ui), "mainWindow", null);
			Glade.XML xml = new Glade.XML ("ui.glade", "mainWindow");
            xml.Autoconnect (this);

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly ();

            //incomingPixbuf = new Gdk.Pixbuf (new System.IO.MemoryStream (Properties.Resources.incoming));
            //outgoingPixbuf = new Gdk.Pixbuf (new System.IO.MemoryStream (Properties.Resources.outgoing));
            //socketPixbuf = new Gdk.Pixbuf (new System.IO.MemoryStream (Properties.Resources.socket));
            //securePixbuf = new Gdk.Pixbuf (new System.IO.MemoryStream (Properties.Resources.secure));
            incomingPixbuf = new Gdk.Pixbuf (asm, "incoming.png");
            outgoingPixbuf = new Gdk.Pixbuf (asm, "outgoing.png");
            socketPixbuf = new Gdk.Pixbuf (asm, "socket.png");
            securePixbuf = new Gdk.Pixbuf (asm, "secure.png");

            processView.AppendColumn ("Process", new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (processView_CellDataFunc));
            processView.Model = processModel;
            processView.Selection.Changed += new EventHandler(processView_SelectionChanged);

            Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
            col.Title = "Resource";
            Gtk.CellRendererPixbuf pbRenderer = new Gtk.CellRendererPixbuf ();
            Gtk.CellRendererText txtRenderer = new Gtk.CellRendererText ();
            col.PackStart (pbRenderer, false);
            col.PackStart (txtRenderer, true);
            col.SetCellDataFunc (pbRenderer, new Gtk.TreeCellDataFunc (resourceView_PixbufCellDataFunc));
            col.SetCellDataFunc (txtRenderer, new Gtk.TreeCellDataFunc (resourceView_TextCellDataFunc));
            resourceView.Selection.Mode = Gtk.SelectionMode.Multiple;
            resourceView.AppendColumn (col);
            resourceView.Model = resourceModel;
            resourceView.Selection.Changed += new EventHandler(resourceView_SelectionChanged);

            transferView.Selection.Mode = Gtk.SelectionMode.Multiple;
            transferView.Model = transferModel;
            transferView.Selection.Mode = Gtk.SelectionMode.Multiple;
            transferView.Selection.Changed += new EventHandler (transferView_SelectionChanged);
            transferModel.SetSortColumnId (0, Gtk.SortType.Ascending);
            transferModel.SetSortFunc (0, new Gtk.TreeIterCompareFunc (transferModel_SortFunc));

            transferView.AppendColumn ("", new Gtk.CellRendererPixbuf (), new Gtk.TreeCellDataFunc (transferView_DirectionCellDataFunc));
            col = transferView.AppendColumn ("From", new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (transferView_FromCellDataFunc));
            col = transferView.AppendColumn ("Size", new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (transferView_SizeCellDataFunc));
            col = transferView.AppendColumn ("When", new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (transferView_TimestampCellDataFunc));
            col = transferView.AppendColumn ("Description", new Gtk.CellRendererText (), new Gtk.TreeCellDataFunc (transferView_DescriptionCellDataFunc));

            chunkView.Model = chunkModel;
            chunkView.DataColIndex = 0;
            chunkView.LinePrefixTextColIndex = 1;
            chunkView.LinePrefixColorColIndex = 2;
            chunkView.Show ();
            mainVPaned.Add2 (chunkView);

            // TODO: investigate why libglade fails to pack the progressbar inside the statusbar
            cancelButton.Image = new Gtk.Image (Gtk.Stock.Cancel, Gtk.IconSize.Button);
            cancelButton.Clicked += new EventHandler (cancelButton_Clicked);
            statusbar.PackStart (progressbar, false, true, 0);
            statusbar.PackStart (cancelButton, false, true, 0);

            // Hook up the async events
            dumpLoader.LoadProgressChanged += delegate (object sender, ProgressChangedEventArgs e) { SetTaskProgress (e.ProgressPercentage); };
            dumpParser.ParseProgressChanged += delegate (object sender, ParseProgressChangedEventArgs e) { SetTaskProgress (e.ProgressPercentage); };

            dumpLoader.LoadCompleted += new LoadCompletedEventHandler (dumpLoader_LoadCompleted);
            dumpParser.ParseCompleted += new ParseCompletedEventHandler (dumpParser_ParseCompleted);
        }

        #endregion // Construction

        #region UI callbacks

        private void window_DeleteEvent (object sender, Gtk.DeleteEventArgs e)
        {
            CancelTask ();
            CloseDump ();
            Gtk.Application.Quit ();
        }

        private void openMenuItem_Activate (object sender, EventArgs e)
        {
            Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog (
                "Choose the file to open", window,
                Gtk.FileChooserAction.Open,
                "Cancel", Gtk.ResponseType.Cancel,
                "Open", Gtk.ResponseType.Accept);

            Gtk.FileFilter ff = new Gtk.FileFilter ();
            ff.AddPattern ("*.osd");
            ff.Name = "oSpy dump files (.osd)";
            fc.AddFilter (ff);

            Gtk.ResponseType response = (Gtk.ResponseType) fc.Run ();
            string filename = fc.Filename;
            fc.Destroy ();

            if (response == Gtk.ResponseType.Accept)
            {
                OpenDump (new BZip2InputStream (File.OpenRead (filename)));
            }
        }

        private void quitMenuItem_Activate (object sender, EventArgs e)
        {
            Gtk.Application.Quit ();
        }

        protected virtual void aboutMenuItem_Activate (object sender, System.EventArgs e)
        {
            Gtk.AboutDialog ad = new Gtk.AboutDialog ();
            ad.Name = "oSpy Studio";
            ad.Website = "http://code.google.com/p/ospy/";
            string[] authors = new string[3];
            authors[0] = "Ole André Vadla Ravnås";
            authors[1] = "Ali Sabil";
            authors[2] = "Johann Prieur";
            ad.Authors = authors;
            ad.Run ();
            ad.Destroy ();
        }

        private void cancelButton_Clicked (object sender, EventArgs e)
        {
            CancelTask ();
        }

        protected virtual void processView_SelectionChanged (object sender, System.EventArgs e)
        {
            resourceModel.Clear ();
            selectedProcesses.Clear ();

            Gtk.TreePath[] selected = processView.Selection.GetSelectedRows ();
            if (selected.Length < 1)
                return;

            Gtk.TreeIter iter;
            if (!processModel.GetIter (out iter, selected[0]))
                return;

            Process selectedProc = processModel.GetValue (iter, 0) as Process;
            if (selectedProc != null)
                selectedProcesses.Add (selectedProc);
            else
                selectedProcesses.AddRange (curProcesses);

            resourceModel.AppendValues (new object[] { null, });
            foreach (Process proc in selectedProcesses)
            {
                foreach (Resource res in proc.Resources)
                {
                    if (res.DataTransfers.Count > 0)
                        resourceModel.AppendValues (new object[] { res, });
                }
            }
        }

        private void processView_CellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

            Process process = model.GetValue (iter, 0) as Process;
            if (process != null)
                textCell.Text = process.ToString ();
            else
                textCell.Text = "(all)";
        }

        protected virtual void resourceView_SelectionChanged (object sender, System.EventArgs e)
        {
            transferModel.Clear ();

            chunkView.Freeze ();
            chunkModel.Clear ();
            chunkView.UnFreeze ();

            Gtk.TreePath[] selectedPaths = resourceView.Selection.GetSelectedRows ();
            if (selectedPaths.Length < 1)
                return;

            List<Resource> selected = new List<Resource> ();
            foreach (Gtk.TreePath path in selectedPaths)
            {
                Gtk.TreeIter iter;
                if (!resourceModel.GetIter (out iter, path))
                    return;

                Resource resource = resourceModel.GetValue (iter, 0) as Resource;
                if (resource == null)
                {
                    selected.Clear ();
                    break;
                }

                selected.Add (resource);
            }

            if (selected.Count == 0)
            {
                foreach (Process proc in selectedProcesses)
                    selected.AddRange (proc.Resources);
            }

            foreach (Resource res in selected)
            {
                foreach (DataTransfer transfer in res.DataTransfers)
                    transferModel.AppendValues (new object[] { transfer, });
            }
        }

        private void resourceView_PixbufCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            Gtk.CellRendererPixbuf pbCell = cell as Gtk.CellRendererPixbuf;
            pbCell.Pixbuf = null;

            Resource res = model.GetValue (iter, 0) as Resource;
            if (res == null)
                return;

            if (res is SocketResource)
                pbCell.Pixbuf = socketPixbuf;
            else if (res is CryptoResource)
                pbCell.Pixbuf = securePixbuf;
        }

        private void resourceView_TextCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

            Resource res = model.GetValue (iter, 0) as Resource;
            if (res != null)
            {
                bool handled = false;

                if (res is SocketResource)
                {
                    SocketResource sockRes = res as SocketResource;

                    if (sockRes.AddressFamily != AddressFamily.Unknown)
                    {
                        textCell.Text = String.Format ("0x{0:x8} - {1}, {2}", sockRes.Handle, sockRes.AddressFamily, sockRes.SocketType);
                        handled = true;
                    }
                }

                if (!handled)
                    textCell.Text = String.Format ("0x{0:x8}", res.Handle);
            }
            else
            {
                textCell.Text = "(all)";
            }
        }

        private void transferView_SelectionChanged (object sender, EventArgs e)
        {
            chunkView.Freeze ();
            chunkModel.Clear ();

            Gtk.TreePath[] selected = transferView.Selection.GetSelectedRows ();
            if (selected.Length > 0)
            {
                foreach (Gtk.TreePath path in selected)
                {
                    Gtk.TreeIter iter;
                    if (!transferModel.GetIter (out iter, path))
                        break;

                    DataTransfer transfer = transferModel.GetValue (iter, 0) as DataTransfer;

                    string linePrefixStr = null;
                    string linePrefixColor = null;
                    if (transfer.Direction == DataDirection.Incoming)
                    {
                        linePrefixStr = "<<";
                        linePrefixColor = "#8ae899";
                    }
                    else
                    {
                        linePrefixStr = ">>";
                        linePrefixColor = "#9cb7d1";
                    }

                    chunkModel.AppendValues (new object[] { transfer.Data, linePrefixStr, linePrefixColor });
                }
            }

            chunkView.UnFreeze ();
        }

        private void transferView_DirectionCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            Gtk.CellRendererPixbuf pbCell = cell as Gtk.CellRendererPixbuf;
            DataTransfer transfer = model.GetValue (iter, 0) as DataTransfer;

            if (transfer.Direction == DataDirection.Incoming)
                pbCell.Pixbuf = incomingPixbuf;
            else
                pbCell.Pixbuf = outgoingPixbuf;
        }

        private void transferView_FromCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            DataTransfer transfer = model.GetValue (iter, 0) as DataTransfer;
            (cell as Gtk.CellRendererText).Text = transfer.FunctionName;
        }

        private void transferView_SizeCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            DataTransfer transfer = model.GetValue (iter, 0) as DataTransfer;
            (cell as Gtk.CellRendererText).Text = Convert.ToString (transfer.Size);
        }

        private void transferView_TimestampCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            DataTransfer transfer = model.GetValue (iter, 0) as DataTransfer;
            (cell as Gtk.CellRendererText).Text = Convert.ToString (curDump.Events[transfer.EventId].Timestamp.ToLongTimeString ());
        }

        private void transferView_DescriptionCellDataFunc (Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
        {
            DataTransfer transfer = model.GetValue (iter, 0) as DataTransfer;
            if (transfer.HasMetaKey ("net.ipv4.remoteEndpoint"))
                (cell as Gtk.CellRendererText).Text = String.Format ("remote: {0}", transfer.GetMetaValue ("net.ipv4.remoteEndpoint"));
            else
                (cell as Gtk.CellRendererText).Text = "";
        }

        private int transferModel_SortFunc (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
        {
            uint i1 = (model.GetValue (a, 0) as DataTransfer).EventId;
            uint i2 = (model.GetValue (b, 0) as DataTransfer).EventId;

            return i1.CompareTo (i2);
        }

        #endregion // UI callbacks

        #region Dump management

        private void OpenDump (Stream dump)
        {
            CloseDump ();

            object state = StartTask ("Opening", delegate (object sender, EventArgs e) {
                dumpLoader.LoadAsyncCancel (sender);
            });

            dumpLoader.LoadAsync (dump, state);
        }

        private void CloseDump ()
        {
            selectedProcesses.Clear ();

            processModel.Clear ();
            resourceModel.Clear ();
            transferModel.Clear ();

            chunkModel.Clear ();

            // TODO: might not be necessary
            processView.Selection.UnselectAll ();
            resourceView.Selection.UnselectAll ();
            transferView.Selection.UnselectAll ();

            if (curProcesses != null)
            {
                foreach (Process process in curProcesses)
                {
                    process.Close ();
                }

                curProcesses = null;
            }

            if (curDump != null)
            {
                curDump.Close ();
                curDump = null;
            }
        }

        #endregion

        #region Async operation callbacks

        private void dumpLoader_LoadCompleted (object sender, LoadCompletedEventArgs e)
        {
            SetTaskCompleted ();

            if (e.Cancelled)
                return;

            try
            {
                curDump = e.Dump;
            }
            catch (Exception ex)
            {
                ShowErrorMessage ("Error opening dump: " + ex.Message);
                return;
            }

            object state = StartTask ("Parsing", delegate (object s, EventArgs ea) {
                dumpParser.ParseAsyncCancel (s);
            });

            dumpParser.ParseAsync (curDump, state);
        }

        private void dumpParser_ParseCompleted (object sender, ParseCompletedEventArgs e)
        {
            SetTaskCompleted ();

            if (e.Cancelled)
                return;

            try
            {
                curProcesses = e.Processes;

                //ShowInfoMessage ("Opened successfully");
            }
            catch (Exception ex)
            {
                ShowErrorMessage ("Error parsing dump: " + ex.Message);
                return;
            }

            processModel.Clear ();
            processModel.AppendValues (new object[] { null, });
            foreach (Process process in curProcesses)
            {
                processModel.AppendValues (new object[] { process, });
            }
        }

        #endregion // Async operation callbacks

        #region Task management helpers

        private object StartTask (string description, EventHandler cancelHandler)
        {
            if (curTask != null)
                throw new InvalidOperationException ("A task is already in progress");

            curTask = new UITask (description, cancelHandler);
            progressbar.Text = description;
            cancelButton.Visible = true;
            progressbar.Visible = true;

            return curTask;
        }

        private void SetTaskProgress (int newProgress)
        {
            progressbar.Fraction = newProgress / 100.0;
        }

        private void SetTaskCompleted ()
        {
            curTask = null;

            cancelButton.Visible = false;
            progressbar.Visible = false;
            progressbar.Text = "";
            progressbar.Fraction = 0.0;
        }

        private void CancelTask ()
        {
            if (curTask != null)
            {
                curTask.Cancel ();
                curTask = null;
            }
        }

        #endregion // Task management helpers

        #region Convenience

        private void ShowInfoMessage (string message)
        {
            ShowMessage (message, Gtk.MessageType.Info);
        }

        private void ShowErrorMessage (string message)
        {
            ShowMessage (message, Gtk.MessageType.Error);
        }

        private void ShowMessage (string message, Gtk.MessageType messageType)
        {
            Gtk.MessageDialog md = new Gtk.MessageDialog (window,
                Gtk.DialogFlags.DestroyWithParent,
                messageType,
                Gtk.ButtonsType.Ok,
                message);
            md.Run ();
            md.Destroy ();
        }

        #endregion
    }

    #region Task management helper class
    internal class UITask
    {
        private string description;
        public string Description
        {
            get { return description; }
        }

        public event EventHandler Canceled;

        public UITask (string description, EventHandler canceled)
        {
            this.description = description;
            Canceled += canceled;
        }

        public void Cancel ()
        {
            Canceled (this, EventArgs.Empty);
        }
    }
    #endregion // Task management helper class
}
