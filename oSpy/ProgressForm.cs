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
            if (curOperationLabel.InvokeRequired || progressBar1.InvokeRequired)
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