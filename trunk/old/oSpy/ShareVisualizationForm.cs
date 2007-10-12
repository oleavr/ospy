//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
