//
// Copyright (c) 2006 Frode Hus <husfro@gmail.com>
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
using oSpy;
using oSpy.Parser;
using oSpy.Net;
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
