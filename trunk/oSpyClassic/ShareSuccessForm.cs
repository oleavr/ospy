using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace oSpyClassic
{
    public partial class ShareSuccessForm : Form
    {
        public ShareSuccessForm(string permalink)
        {
            InitializeComponent();

            permalinkTextBox.Text = permalink;
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }
    }
}