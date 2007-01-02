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
using System.Threading;

namespace oSpy
{
    public partial class ProgressForm : Form, IProgressFeedback
    {
        public ProgressForm(string operation)
        {
            InitializeComponent();

            ProgressUpdate(operation, 0);
        }

        public delegate void ProgressUpdateHandler(string operation, int progress);

        public void ProgressUpdate(string operation, int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new ProgressUpdateHandler(ProgressUpdate), operation, progress);
            }
            else
            {
                curOperationLabel.Text = operation;
                progressBar1.Value = progress;
            }
        }

        public void OperationComplete()
        {
            Invoke(new ThreadStart(SetDialogResult));
        }

        private void SetDialogResult()
        {
            this.DialogResult = DialogResult.OK;
        }
    }

    public interface IProgressFeedback
    {
        void ProgressUpdate(string operation, int progress);
        void OperationComplete();
    }
}