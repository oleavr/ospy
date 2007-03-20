//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using ICSharpCode.SharpZipLib.BZip2;
using System.IO;
using System.Xml;
using oSpy.Configuration;

namespace oSpy
{
    public partial class ShareVisualizationForm : Form
    {
        protected ConfigContext config;

        protected ConversationsForm convForm;

        public ShareVisualizationForm(ConversationsForm convForm)
        {
            InitializeComponent();

            this.convForm = convForm;

            config = ConfigManager.GetContext("SubmitVisualizationForm");

            if (config.HasSetting("Name"))
                nameTextBox.Text = (string) config["Name"];

            UpdateControls();
        }

        private void submitBtn_Click(object sender, EventArgs e)
        {
            XmlDocument doc = convForm.ExportToXml();

            MemoryStream memStream = new MemoryStream();
            BZip2OutputStream bzStream = new BZip2OutputStream(memStream);
            doc.Save(bzStream);
            bzStream.Close();
            memStream.Close();

            try
            {
                oSpyRepository.RepositoryService svc = new oSpy.oSpyRepository.RepositoryService();
                string permalink = svc.SubmitTrace(nameTextBox.Text, descTextBox.Text, memStream.ToArray());

                ShareSuccessForm frm = new ShareSuccessForm(permalink);
                frm.ShowDialog(this);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to submit visualization: {0}", ex.Message),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void descTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        protected void UpdateControls()
        {
            bool enabled = true;

            if (nameTextBox.Text.Length == 0)
                enabled = false;
            if (descTextBox.Text.Length == 0)
                enabled = false;

            submitBtn.Enabled = enabled;
        }

        private void SubmitVisualizationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;

            config["Name"] = nameTextBox.Text;
        }
    }
}