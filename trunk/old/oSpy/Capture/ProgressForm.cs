//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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

namespace oSpy.Capture
{
    public partial class ProgressForm : Form
    {
        private Manager captureMgr;

        public ProgressForm(Manager captureMgr)
        {
            InitializeComponent();

            this.captureMgr = captureMgr;
        }

        private void pollTimer_Tick(object sender, EventArgs e)
        {
            int evCount, evBytes;

            captureMgr.GetCaptureStatistics(out evCount, out evBytes);

            evCountLabel.Text = Convert.ToString(evCount);
            evBytesLabel.Text = Convert.ToString(evBytes);
        }

        private void CaptureProgressForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            pollTimer.Enabled = false;
        }
    }
}
