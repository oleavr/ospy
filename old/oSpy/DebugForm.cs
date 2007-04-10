//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using oSpy.Util;

namespace oSpy
{
    public partial class DebugForm : Form, DebugLogger
    {
        private int numLines = 0;

        public DebugForm()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public void AddMessage(string msg)
        {
            string newLine = String.Format("{0}\r\n", msg);

            /*
            if (numLines >= 1000)
            {
                string text = textBox.Text;
                int pos = text.IndexOf("\r\n");
                textBox.Text = text.Substring(pos + 2) + newLine;
            }
            else
            {*/
                textBox.Text += newLine;
            //}

            numLines++;
        }
        public void AddMessage(string msg, params object[] vals) {
            string newLine = String.Format(msg + "\r\n", vals);
            textBox.Text += newLine;
            numLines++;
        }
        private void clearButton_Click(object sender, EventArgs e)
        {
            textBox.Text = "";
        }

        private void DebugForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}