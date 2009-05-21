//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.ComponentModel;
using System.IO;
using Gtk;
using oSpy.SharpDumpLib;

public partial class MainWindow : Gtk.Window
{
    private DumpLoader m_dumpLoader = new DumpLoader();

    private Task m_curTask = null;
    private Dump m_curDump = null;

    public MainWindow()
        : base(Gtk.WindowType.Toplevel)
    {
        Build();

        m_dumpLoader.LoadCompleted += dumpLoader_LoadCompleted;
        m_dumpLoader.LoadProgressChanged += dumpLoader_LoadProgressChanged;
    }

    private void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    private void OpenAction_Activated(object sender, System.EventArgs e)
    {
        FileChooserDialog dialog = new FileChooserDialog("Choose the file to open", this, FileChooserAction.Open,
                                                         "Cancel", Gtk.ResponseType.Cancel,
                                                         "Open", Gtk.ResponseType.Accept);

        FileFilter filter = new FileFilter();
        filter.Name = "oSpy dump files (.osd)";
        filter.AddPattern("*.osd");
        dialog.AddFilter(filter);

        ResponseType response = (ResponseType) dialog.Run();
        string filename = dialog.Filename;
        dialog.Destroy ();

        if (response == Gtk.ResponseType.Accept)
        {
            OpenDump(File.OpenRead(filename));
        }
    }

    private void QuitAction_Activated(object sender, System.EventArgs e)
    {
        Destroy();
    }

    private void OpenDump(Stream stream)
    {
        StartTask("Opening", delegate(object sender, EventArgs e)
                             {
                                 m_dumpLoader.LoadAsync(stream, sender);
                             },
                             delegate(object sender, EventArgs e)
                             {
                                 m_dumpLoader.LoadAsyncCancel(sender);
                             });
    }

    private void dumpLoader_LoadCompleted(object sender, LoadCompletedEventArgs e)
    {
        if (m_curTask != null)
            m_curTask.Completed();

        if (e.Cancelled)
            return;

        try
        {
            Dump newDump = e.Dump;

            if (m_curDump != null)
                m_curDump = null; // Dummy placeholder, switch dump here
            m_curDump = newDump;
        }
        catch (Exception ex)
        {
            ShowErrorMessage("Error opening dump. Please file a bug.\n\nDetails:\n" + ex.ToString());
        }
    }

    private void dumpLoader_LoadProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (m_curTask != null)
            m_curTask.SetProgress(e.ProgressPercentage);
    }

    /*
    private void ShowInfoMessage(string message)
    {
        ShowMessage(message, MessageType.Info);
    }
    */

    private void ShowErrorMessage(string message)
    {
        ShowMessage(message, MessageType.Error);
    }

    private void ShowMessage(string message, MessageType messageType)
    {
        MessageDialog dialog = new MessageDialog(this, DialogFlags.DestroyWithParent, messageType, ButtonsType.Ok,
                                                 message);
        dialog.Run();
        dialog.Destroy();
    }

    private void StartTask(string description, EventHandler started, EventHandler cancelled)
    {
        if (m_curTask != null)
            throw new InvalidOperationException("A task is already in progress");

        m_curTask = new Task(this, description, started, cancelled);
        m_curTask.Start();
    }

    private void cancelButton_Clicked(object sender, EventArgs e)
    {
        m_curTask.Cancel();
    }

    private class Task
    {
        private MainWindow m_window;
        private string m_description;

        public string Description
        {
            get
            {
                return m_description;
            }
        }

        public event EventHandler Started;
        public event EventHandler Cancelled;

        public Task(MainWindow window, string description, EventHandler started, EventHandler cancelled)
        {
            m_window = window;
            m_description = description;

            Started += started;
            Cancelled += cancelled;
        }

        public void Start()
        {
            m_window.progressbar.Text = m_description;
            m_window.cancelButton.Visible = true;
            m_window.progressbar.Visible = true;

            Started(this, EventArgs.Empty);
        }

        public void SetProgress(int newProgress)
        {
            m_window.progressbar.Fraction = newProgress / 100.0;
        }

        public void Completed()
        {
            m_window.m_curTask = null;

            m_window.cancelButton.Visible = false;
            m_window.progressbar.Visible = false;
            m_window.progressbar.Text = "";
            m_window.progressbar.Fraction = 0.0;
        }

        public void Cancel()
        {
            Cancelled(this, EventArgs.Empty);

            Completed();
        }
    }
}