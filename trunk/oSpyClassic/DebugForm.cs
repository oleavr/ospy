/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using oSpyClassic.Util;

namespace oSpyClassic
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