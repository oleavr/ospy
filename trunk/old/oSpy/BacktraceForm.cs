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