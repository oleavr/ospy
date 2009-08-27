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

namespace oSpy.Capture
{
    internal partial class ProgressForm : Form
    {
        private EventHandler statsChangedHandler;

        public ProgressForm(Manager manager)
        {
            InitializeComponent();

            statsChangedHandler = manager_EventStatsChanged;
            manager.EventStatsChanged += statsChangedHandler;
        }

        private void manager_EventStatsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(statsChangedHandler, sender, e);
                return;
            }

            Manager.EventStats stats = (sender as Manager).Stats;
            msgCountLabel.Text = Convert.ToString(stats.MessageCount);
            msgBytesLabel.Text = Convert.ToString(stats.MessageBytes);
            pktCountLabel.Text = Convert.ToString(stats.PacketCount);
            pktBytesLabel.Text = Convert.ToString(stats.PacketBytes);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}