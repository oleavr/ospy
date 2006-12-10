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

namespace oSpy
{
    public partial class CaptureForm : Form
    {
        private AgentListener listener;

        //private int pktCount, pktBytes;

        private AgentListener.BlocksReceivedHandler receivedHandler;

        public CaptureForm(AgentListener listener)
        {
            InitializeComponent();

            this.listener = listener;
            //pktCount = pktBytes = 0;

            receivedHandler = new AgentListener.BlocksReceivedHandler(listener_BlocksReceived);
            listener.BlocksReceived += receivedHandler;

            listener.Start();
        }

        private void listener_BlocksReceived(int newBlockCount, int newBlockSize)
        {
            if (InvokeRequired)
            {
                Invoke(receivedHandler, new object[] { newBlockCount, newBlockSize });
                return;
            }

            msgCountLabel.Text = Convert.ToString(newBlockCount);
            msgBytesLabel.Text = Convert.ToString(newBlockSize);
            //pktCountLabel.Text = Convert.ToString(pktCount);
            //pktBytesLabel.Text = Convert.ToString(pktBytes);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            listener.Stop();

            DialogResult = DialogResult.OK;
        }
    }
}