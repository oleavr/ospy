//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.IO;
using System.Net;

namespace oSpy
{
    public partial class SoftwallForm : Form
    {
        protected static Dictionary<string, int> standardDefines;

        protected static int DEFAULT_RETURN_VALUE;
        protected static int DEFAULT_LAST_ERROR;

        static SoftwallForm()
        {
            standardDefines = new Dictionary<string, int>();
            standardDefines["SOCKET_ERROR"] = -1;

            standardDefines["WSAEADDRINUSE"] = 10048;
            standardDefines["WSAEADDRNOTAVAIL"] = 10049;
            standardDefines["WSAENETDOWN"] = 10050;
            standardDefines["WSAENETUNREACH"] = 10051;
            standardDefines["WSAETIMEDOUT"] = 10060;
            standardDefines["WSAECONNREFUSED"] = 10061;
            standardDefines["WSAEHOSTUNREACH"] = 10065;

            DEFAULT_RETURN_VALUE = standardDefines["SOCKET_ERROR"];
            DEFAULT_LAST_ERROR = standardDefines["WSAEHOSTUNREACH"];
        }

        public SoftwallForm()
        {
            InitializeComponent();

            processNameCBox.Tag = processNameTBox;
            funcNameCBox.Tag = funcNameTBox;
            retAddrCBox.Tag = retAddrTBox;
            localAddrCBox.Tag = localAddrTBox;
            localPortCBox.Tag = localPortTBox;
            remoteAddrCBox.Tag = remoteAddrTBox;
            remotePortCBox.Tag = remotePortTBox;

            LoadRules();
        }

        private void LoadRules()
        {
            ruleListView.Items.Clear();

            string path = GetRulesFilePath ();
            if (!File.Exists (path))
                return;

            softwallDataSet.ReadXml (path);

            foreach (DataRow row in softwallDataSet.Tables[0].Rows)
            {
                ruleListView.Items.Add(row["Name"] as string);
            }
        }

        private void SaveRules()
        {
            softwallDataSet.WriteXml(GetRulesFilePath());
        }

        private string GetRulesFilePath()
        {
            string appDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);

            return String.Format("{0}\\softwall.osr", appDir);
        }

        private void ResetUI()
        {
            conditionsGroupBox.Enabled = ruleListView.Items.Count > 0;
            actionsGroupBox.Enabled = ruleListView.Items.Count > 0;

            if (ruleListView.Items.Count == 0)
            {
                processNameCBox.CheckState = CheckState.Unchecked;
                funcNameCBox.CheckState = CheckState.Unchecked;
                retAddrCBox.CheckState = CheckState.Unchecked;
                localAddrCBox.CheckState = CheckState.Unchecked;
                localPortCBox.CheckState = CheckState.Unchecked;
                remoteAddrCBox.CheckState = CheckState.Unchecked;
                remotePortCBox.CheckState = CheckState.Unchecked;

                processNameTBox.Text = "";
                funcNameTBox.Text = "";
                retAddrTBox.Text = "";
                localAddrTBox.Text = "";
                localPortTBox.Text = "";
                remoteAddrTBox.Text = "";
                remotePortTBox.Text = "";
            }
        }

        private void conditionCBox_CheckStateChanged(object sender, EventArgs e)
        {
            CheckBox box = sender as CheckBox;

            TextBox boundControl = box.Tag as TextBox;
            boundControl.Enabled = box.CheckState != CheckState.Unchecked;

            if (box.CheckState != CheckState.Checked)
            {
                boundControl.Text = "";
                DataTable tbl = softwallDataSet.Tables[0];
                CurrencyManager cm = (CurrencyManager) this.BindingContext[softwallDataSet, "Rules"];
                if (cm.Position >= 0)
                {
                    string colName = boundControl.DataBindings[0].BindingMemberInfo.BindingField;
                    tbl.Rows[cm.Position][colName] = DBNull.Value;
                }
            }
        }

        private void ruleListMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            deleteToolStripMenuItem.Enabled = (ruleListView.SelectedIndices.Count > 0);
        }

        private bool addingRow;

        private void newRuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addingRow = true;

            ListViewItem newItem = new ListViewItem("");
            ruleListView.Items.Add(newItem);
            newItem.BeginEdit();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = ruleListView.SelectedIndices[0];

            ruleListView.Items.RemoveAt(index);

            DataTable tbl = softwallDataSet.Tables[0];
            tbl.Rows.RemoveAt(index);

            ResetUI();
        }

        private void rulesListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null || e.Label == "")
            {
                e.CancelEdit = true;

                if (addingRow)
                {
                    ruleListView.Items.RemoveAt(e.Item);
                    ResetUI();
                    addingRow = false;
                }

                return;
            }

            DataTable tbl = softwallDataSet.Tables[0];


            if (addingRow)
            {
                DataRow newRow = tbl.NewRow();
                newRow["Name"] = e.Label;
                newRow["ReturnValue"] = DEFAULT_RETURN_VALUE;
                newRow["LastError"] = DEFAULT_LAST_ERROR;
                tbl.Rows.Add(newRow);

                ruleListView.SelectedIndices.Clear();
                ruleListView.SelectedIndices.Add(e.Item);
            }
            else
            {
                DataRow row = tbl.Rows[e.Item];
                row["Name"] = e.Label;
            }

            addingRow = false;
        }

        private void rulesListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            int index = e.ItemIndex;

            DataTable tbl = softwallDataSet.Tables[0];

            ResetUI();

            if (index < tbl.Rows.Count)
            {
                CurrencyManager cm = (CurrencyManager) this.BindingContext[softwallDataSet, "Rules"];
                cm.Position = index;

                DataRow row = tbl.Rows[index];
                SetCheckStateFromValue(processNameCBox, row["ProcessName"]);
                SetCheckStateFromValue(funcNameCBox, row["FunctionName"]);
                SetCheckStateFromValue(retAddrCBox, row["ReturnAddress"]);
                SetCheckStateFromValue(localAddrCBox, row["LocalAddress"]);
                SetCheckStateFromValue(localPortCBox, row["LocalPort"]);
                SetCheckStateFromValue(remoteAddrCBox, row["RemoteAddress"]);
                SetCheckStateFromValue(remotePortCBox, row["RemotePort"]);
            }
        }

        private void SetCheckStateFromValue(CheckBox box, object value)
        {
            box.CheckState = (!(value is DBNull))
                ? CheckState.Checked : CheckState.Unchecked;
        }

        private void SoftwallForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveRules();
        }

        public void AddRule(string processName, string functionName,
                            uint returnAddress,
                            string localAddress, ushort localPort,
                            string remoteAddress, ushort remotePort)
        {
            DataTable tbl = softwallDataSet.Tables[0];
            DataRow row = tbl.NewRow();

            string name = String.Format("Rule{0:00}", ruleListView.Items.Count + 1);

            row["Name"] = name;
            row["ProcessName"] = processName;
            row["FunctionName"] = functionName;

            if (returnAddress != 0)
                row["ReturnAddress"] = returnAddress;

            if (localAddress != null)
                row["LocalAddress"] = localAddress;
            if (localPort > 0)
                row["LocalPort"] = localPort;

            if (remoteAddress != null)
                row["RemoteAddress"] = remoteAddress;
            if (remotePort > 0)
                row["RemotePort"] = remotePort;

            row["ReturnValue"] = DEFAULT_RETURN_VALUE;
            row["LastError"] = DEFAULT_LAST_ERROR;

            tbl.Rows.Add(row);
            ruleListView.Items.Add(name);

            ruleListView.SelectedIndices.Clear();
            ruleListView.SelectedIndices.Add(ruleListView.Items.Count - 1);
        }

        public Softwall.Rule[] GetRules()
        {
            DataRowCollection rows = softwallDataSet.Tables[0].Rows;

            List<Softwall.Rule> rules = new List<Softwall.Rule>(rows.Count);
            foreach (DataRow row in rows)
            {
                Softwall.Rule rule = new Softwall.Rule();

                rule.Conditions = 0;

                object obj = row["ProcessName"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_PROCESS_NAME;
                    rule.ProcessName = obj as string;
                }

                obj = row["FunctionName"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_FUNCTION_NAME;
                    rule.FunctionName = obj as string;
                }

                obj = row["ReturnAddress"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_RETURN_ADDRESS;
                    rule.ReturnAddress = (UInt32) obj;
                }

                obj = row["LocalAddress"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_LOCAL_ADDRESS;
                    rule.LocalAddress = IPAddrFromStr(obj as string);
                }

                obj = row["LocalPort"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_LOCAL_PORT;
                    rule.LocalPort = PortToBigEndian((UInt16) obj);
                }

                obj = row["RemoteAddress"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_PEER_ADDRESS;
                    rule.RemoteAddress = IPAddrFromStr(obj as string);
                }

                obj = row["RemotePort"];
                if (!(obj is DBNull))
                {
                    rule.Conditions |= Softwall.CONDITION_PEER_PORT;
                    rule.RemotePort = PortToBigEndian((UInt16) obj);
                }

                rule.Retval = (Int32) row["ReturnValue"];
                rule.LastError = (UInt32) row["LastError"];

                rules.Add(rule);
            }

            return rules.ToArray();
        }

        private UInt32 IPAddrFromStr(string str)
        {
            byte[] bytes = IPAddress.Parse(str).GetAddressBytes();

            return (UInt32)((bytes[3] << 24) |
                            (bytes[2] << 16) |
                            (bytes[1] << 8) |
                            (bytes[0] << 0));
        }

        private UInt16 PortToBigEndian(UInt16 port)
        {
            return (UInt16) (((port & 0xff) << 8) |
                             ((port >> 8) & 0xff));
        }

        private void retValLastErrorCBox_Validating(object sender, CancelEventArgs e)
        {
            ComboBox box = sender as ComboBox;

            string str = box.Text.Split(new char[] { ' ' }, 2)[0];

            if (standardDefines.ContainsKey(str))
            {
                box.Text = Convert.ToString(standardDefines[str]);
            }
        }
    }
}
