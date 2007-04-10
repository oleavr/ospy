//
// Copyright (c) 2006 Frode Hus <husfro@gmail.com>
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