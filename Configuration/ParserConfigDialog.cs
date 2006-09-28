using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using oSpy;
using oSpy.Parser;
namespace oSpy.Configuration
{
    public partial class ParserConfigDialog : Form
    {
        private PacketParser packetParser;
        public ParserConfigDialog(PacketParser packetParser)
        {
            InitializeComponent();
            this.packetParser = packetParser;
            populateParsers();
        }

        private void populateParsers() {
            comboParsers.Items.Clear();
            foreach (TransactionFactory fac in packetParser.Factories) {
                comboParsers.Items.Add(fac.Name());
            }
        }
        private void saveSettings() {
            if (comboParsers.Text.Equals(""))
                return;
            string factory = comboParsers.Text;
            Setting setting;
            List<Setting> settings = new List<Setting>();
            foreach (DataGridViewRow row in dgSettings.Rows) {
                setting = new Setting();
                setting.Property = row.Cells[0].Value.ToString();
                setting.Value = row.Cells[1].Value.ToString();
                settings.Add(setting);
            }
            ParserConfiguration.Settings.Add(factory, settings);
        }


        private void btnSave_Click(object sender, EventArgs e) {
           
            saveSettings();
        }
    }
}