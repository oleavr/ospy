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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace oSpy.Capture
{
    public partial class StartForm : Form
    {
        private static ICollection<ListViewItem> suggestItems = null;
        private static ImageList suggestImages = null;

        public StartForm()
        {
            InitializeComponent();

            UpdateUi();

            if (suggestItems == null)
            {
                Thread th = new Thread(CreateSuggestions);
                th.Start();
            }
            else
            {
                searchBox.UpdateSuggestions(suggestItems, suggestImages);
            }
        }

        public StartDetails GetDetails()
        {
            StartDetails details = null;

            if (ShowDialog() == DialogResult.OK)
            {
                ProcessStartInfo info = new ProcessStartInfo(filenameBox.Text, argsBox.Text);
                info.WorkingDirectory = startInBox.Text;

                details = new StartDetails(info);
            }

            return details;
        }

        private void UpdateUi()
        {
            bool enabled = true;

            if (filenameBox.Text.Length == 0)
                enabled = false;
            else if (!filenameBox.Text.EndsWith(".exe"))
                enabled = false;
            else if (!File.Exists(filenameBox.Text))
                enabled = false;
            else if (startInBox.Text.Length > 0 && !Directory.Exists(startInBox.Text))
                enabled = false;

            startBtn.Enabled = enabled;
        }

        private void CreateSuggestions()
        {
            List<ApplicationListViewItem> items = new List<ApplicationListViewItem>();
            ImageList imageLst = new ImageList();
            imageLst.ColorDepth = ColorDepth.Depth32Bit;

            StringBuilder allUsersStartMenu = new StringBuilder(260 + 1);
            WinApi.SHGetSpecialFolderPath(IntPtr.Zero, allUsersStartMenu, WinApi.CSIDL.COMMON_PROGRAMS, false);

            Stack<DirectoryInfo> pendingDirs = new Stack<DirectoryInfo>();
            pendingDirs.Push(new DirectoryInfo(allUsersStartMenu.ToString()));
            pendingDirs.Push(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)));

            IWshRuntimeLibrary.IWshShell shell = new IWshRuntimeLibrary.WshShell();

            while (pendingDirs.Count > 0)
            {
                DirectoryInfo dir = pendingDirs.Pop();

                try
                {
                    foreach (FileInfo file in dir.GetFiles("*.lnk"))
                    {
                        var shortcut = shell.CreateShortcut(file.FullName) as IWshRuntimeLibrary.IWshShortcut;
                        if (shortcut != null)
                        {
                            string name = Path.GetFileNameWithoutExtension(shortcut.FullName);
                            string targetName = Path.GetFileNameWithoutExtension(shortcut.TargetPath);
                            string targetExt = Path.GetExtension(shortcut.TargetPath);
                            if (targetExt == ".exe" && name.ToLower().IndexOf("uninst") < 0 && targetName.ToLower().IndexOf("uninst") < 0)
                            {
                                var item = new ApplicationListViewItem(name, shortcut.TargetPath, shortcut.Arguments, shortcut.WorkingDirectory);

                                var icon = System.Drawing.Icon.ExtractAssociatedIcon(item.FileName);
                                imageLst.Images.Add(item.FileName, icon);
                                item.ImageKey = item.FileName;

                                items.Add(item);
                            }
                        }
                    }

                    foreach (DirectoryInfo subdir in dir.GetDirectories())
                        pendingDirs.Push(subdir);
                }
                catch
                {
                }
            }

            items.Sort();

            suggestItems = items.ToArray();
            suggestImages = imageLst;

            searchBox.UpdateSuggestions(suggestItems, suggestImages);
        }

        private void searchBox_SuggestionActivated(object sender, SuggestionActivatedEventArgs e)
        {
            ApplicationListViewItem item = e.Item as ApplicationListViewItem;
            filenameBox.Text = item.FileName;
            argsBox.Text = item.Arguments;
            startInBox.Text = item.WorkingDirectory;

            UpdateUi();
            startBtn.Focus();
        }

        private void anyFileTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void browseBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filenameBox.Text = openFileDialog.FileName;
                UpdateUi();
            }
        }
    }

    internal class ApplicationListViewItem : ListViewItem, IComparable
    {
        private string fileName;
        private string arguments;
        private string workingDirectory;

        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        public string Arguments
        {
            get
            {
                return arguments;
            }
        }

        public string WorkingDirectory
        {
            get
            {
                return workingDirectory;
            }
        }

        public ApplicationListViewItem(string displayName, string fileName, string arguments, string workingDirectory)
            : base(displayName)
        {
            this.fileName = fileName;
            this.arguments = arguments;
            this.workingDirectory = workingDirectory;
        }

        public int CompareTo(Object obj)
        {
            ApplicationListViewItem other = obj as ApplicationListViewItem;

            return Text.CompareTo(other.Text);
        }
    }
}
