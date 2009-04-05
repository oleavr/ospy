/**
 * Copyright (C) 2009  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System.Windows.Forms;
using System.Reflection;

namespace oSpy
{
    public partial class WelcomeForm : Form
    {
        public WelcomeForm ()
        {
            InitializeComponent ();

            versionLabel.Text = AssemblyVersion;
            copyrightLabel.Text = AssemblyCopyright;
        }

        public bool ShowOnStartupChecked
        {
            get { return showOnStartupCheckBox.Checked; }
            set { showOnStartupCheckBox.Checked = value; }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly ().GetName ().Version.ToString ();
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly ().GetCustomAttributes (typeof (AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                    return "";
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        private void donateButton_Click (object sender, System.EventArgs e)
        {
            string business = "oleavr@gmail.com";
            string description = "Donation.";
            string country = "NO";
            string currency = "EUR";

            string url = "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "\"" + "_donations" + "\"" +
                "&business=" + "\"" + business + "\"" +
                "&lc=" + "\"" + country + "\"" +
                "&item_name=" + "\"" + description + "\"" +
                "&currency_code=" + "\"" + currency + "\"" +
                "&bn=" + "\"" + "PP%2dDonationsBF" + "\"";

            Help.ShowHelp (this, url);
        }
    }
}
