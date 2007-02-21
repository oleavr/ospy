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
            MemoryStream memStream = new MemoryStream();
            BZip2OutputStream compStream = new BZip2OutputStream(memStream);
            XmlDocument doc = convForm.ExportToXml();
            doc.Save(compStream);

            try
            {
                oSpyRepository.RepositoryService svc = new oSpy.oSpyRepository.RepositoryService();
                svc.SubmitTrace(nameTextBox.Text, descTextBox.Text, memStream.ToArray());

                MessageBox.Show("Visualization submitted successfully!\nThanks for sharing!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

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