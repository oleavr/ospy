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

namespace oSpy
{
    public partial class CaptureProgressForm : Form
    {
        private AgentListener listener;

        private uint msgCount, msgBytes;
        private uint pktCount, pktBytes;

        private AgentListener.ElementsReceivedHandler receivedHandler;

        public CaptureProgressForm(AgentListener listener, AgentListener.SoftwallRule[] rules)
        {
            InitializeComponent();

            this.listener = listener;
            msgCount = msgBytes = 0;
            pktCount = pktBytes = 0;

            receivedHandler = new AgentListener.ElementsReceivedHandler(listener_MessageElementsReceived);
            listener.MessageElementsReceived += receivedHandler;

            listener.Start(rules);
        }

        private void listener_MessageElementsReceived(AgentListener.MessageQueueElement[] elements)
        {
            if (InvokeRequired)
            {
                Invoke(receivedHandler, new object[] { elements });
                return;
            }

            foreach (AgentListener.MessageQueueElement el in elements)
            {
                if (el.msg_type == MessageType.MESSAGE_TYPE_MESSAGE)
                {
                    msgCount++;
                    msgBytes += (uint) el.message.Length;
                }
                else
                {
                    pktCount++;
                    pktBytes += el.len;
                }
            }

            msgCountLabel.Text = Convert.ToString(msgCount);
            msgBytesLabel.Text = Convert.ToString(msgBytes);
            pktCountLabel.Text = Convert.ToString(pktCount);
            pktBytesLabel.Text = Convert.ToString(pktBytes);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            listener.Stop();

            DialogResult = DialogResult.OK;
        }
    }
}