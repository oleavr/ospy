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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace oSpy
{
    public partial class BacktraceForm : Form
    {
        public BacktraceForm(Int32 index, string functionName, string backtrace)
        {
            InitializeComponent();

            Text = String.Format("Backtrace for #{0} - {1}", index, functionName);

            string[] lines = backtrace.Split(new char[] { '\n' });
            foreach (string line in lines)
            {
                btListBox.Items.Add(line);
            }

            btListBox.SelectedIndex = 0;
        }

        private void goToInIdaBtn_Click(object sender, EventArgs e)
        {
            string line = (string) btListBox.SelectedItem;
            string[] tokens = line.Split(new string[] { "::" }, 2, StringSplitOptions.None);
            string[] subTokens = tokens[1].Split(new char[] { ' ' }, 2);
            Util.IDA.GoToAddressInIDA(tokens[0], Convert.ToUInt32(subTokens[0].Substring(2), 16));
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string line = (string)btListBox.SelectedItem;
            string[] tokens = line.Split(new string[] { "::" }, 2, StringSplitOptions.None);

            Clipboard.SetText(tokens[1]);
        }
    }
}
