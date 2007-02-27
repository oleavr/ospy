using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace oSpy
{
    public partial class BacktraceForm : Form
    {
        public BacktraceForm(Int32 index, string functionName, string backtrace)
        {
            InitializeComponent();

            Text = String.Format("Backtrace for #{0} - {1}", index, functionName);

            string[] lines = backtrace.Split(new char[] { '\n' });
            foreach (string line in lines)
            {
                btListBox.Items.Add(line);
            }

            btListBox.SelectedIndex = 0;
        }

        private void goToInIdaBtn_Click(object sender, EventArgs e)
        {
            string line = (string) btListBox.SelectedItem;
            string[] tokens = line.Split(new string[] { "::" }, 2, StringSplitOptions.None);
            string[] subTokens = tokens[1].Split(new char[] { ' ' }, 2);
            Util.IDA.GoToAddressInIDA(tokens[0], Convert.ToUInt32(subTokens[0].Substring(2), 16));
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string line = (string)btListBox.SelectedItem;
            string[] tokens = line.Split(new string[] { "::" }, 2, StringSplitOptions.None);

            Clipboard.SetText(tokens[1]);
        }
    }
}