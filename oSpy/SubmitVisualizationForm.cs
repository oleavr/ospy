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
    public partial class SubmitVisualizationForm : Form
    {
        protected ConfigContext config;

        protected ConversationsForm convForm;

        public SubmitVisualizationForm(ConversationsForm convForm)
        {
            InitializeComponent();

            this.convForm = convForm;

            config = ConfigManager.GetContext("SubmitVisualizationForm");

            if (config.HasSetting("Name"))
                nameTextBox.Text = (string) config["Name"];
            if (config.HasSetting("E-mail"))
                emailTextBox.Text = (string) config["E-mail"];

            UpdateControls();
        }

        private void tagsAddBtn_Click(object sender, EventArgs e)
        {
            tagsListBox.Items.Add(addTagTextBox.Text);
        }

        private void submitBtn_Click(object sender, EventArgs e)
        {
            oSpyRepository.RepositoryService svc = new oSpy.oSpyRepository.RepositoryService();

            StringBuilder tagsBuilder = new StringBuilder();
            foreach (string tag in tagsListBox.Items)
            {
                if (tagsBuilder.Length > 0)
                    tagsBuilder.Append(":");
                tagsBuilder.Append(tag);
            }

            MemoryStream memStream = new MemoryStream();
            BZip2OutputStream compStream = new BZip2OutputStream(memStream);
            XmlDocument doc = convForm.ExportToXml();
            doc.Save(compStream);

            try
            {
                svc.SubmitTrace(nameTextBox.Text, emailTextBox.Text, descTextBox.Text,
                    tagsBuilder.ToString(), memStream.ToArray());

                MessageBox.Show("Visualization submitted successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Failed to submit visualization: {0}", ex.Message),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tagsDelBtn_Click(object sender, EventArgs e)
        {
            if (tagsListBox.SelectedIndex >= 0)
                tagsListBox.Items.RemoveAt(tagsListBox.SelectedIndex);
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void emailTextBox_TextChanged(object sender, EventArgs e)
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
            if (emailTextBox.Text.Length == 0)
                enabled = false;
            if (descTextBox.Text.Length == 0)
                enabled = false;
            if (descTextBox.Text.Length == 0)
                enabled = false;

            submitBtn.Enabled = enabled;
        }

        private void SubmitVisualizationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;

            config["Name"] = nameTextBox.Text;
            config["E-mail"] = emailTextBox.Text;
        }
    }
}