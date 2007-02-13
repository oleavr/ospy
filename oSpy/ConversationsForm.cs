/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using oSpy.Parser;
using oSpy.Net;
using oSpy.Util;

namespace oSpy
{
    public partial class ConversationsForm : Form
    {
        private IPSession[] sessions;

        public ConversationsForm(IPSession[] sessions)
        {
            InitializeComponent();

            multiStreamView.Visible = false;

            this.sessions = sessions;

            multiStreamView.TransactionDoubleClick += new TransactionDoubleClickHandler(multiStreamView_TransactionDoubleClick);
        }

        private void multiStreamView_Click(object sender, EventArgs e)
        {
            propertyGrid.SelectedObject = null;
            splitContainer.Panel2Collapsed = true;
        }

        private void multiStreamView_TransactionDoubleClick(VisualTransaction transaction)
        {
            propertyGrid.SelectedObject = transaction;
            splitContainer.Panel2Collapsed = false;
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                multiStreamView.Visible = false;

                Stream stream = File.Open(openFileDialog.FileName, FileMode.Open);
                BinaryFormatter bFormatter = new BinaryFormatter();
                multiStreamView.Sessions = (VisualSession[])bFormatter.Deserialize(stream);
                stream.Close();

                multiStreamView.Visible = true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Stream stream = File.Open(saveFileDialog.FileName, FileMode.Create);
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, multiStreamView.Sessions);
                stream.Close();
            }
        }

        private void exportToimageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (exportToImageFileDialog.ShowDialog() == DialogResult.OK)
            {
                multiStreamView.SaveToPng(exportToImageFileDialog.FileName);
            }
        }

        private void closeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewVisualizationForm frm = new NewVisualizationForm();

            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                multiStreamView.Visible = false;

                multiStreamView.Clear();

                List<VisualSession> visSessions = new List<VisualSession>(sessions.Length);
                List<VisualTransaction> transactions = new List<VisualTransaction>();

                foreach (IPSession session in sessions)
                {
                    foreach (SessionVisualizer vis in frm.GetSelectedVisualizers())
                    {
                        transactions.AddRange(vis.GetTransactions(session));
                    }

                    if (transactions.Count > 0)
                    {
                        VisualSession vs = new VisualSession(session);

                        transactions.AddRange(StreamVisualizationManager.TCPEventsVis.GetTransactions(session));

                        vs.Transactions.AddRange(transactions);

                        vs.TransactionsCreated();

                        visSessions.Add(vs);

                        transactions.Clear();
                    }
                }

                foreach (VisualSession session in visSessions)
                {
                    session.SessionsCreated();
                }

                multiStreamView.Sessions = visSessions.ToArray();

                multiStreamView.Visible = true;
                multiStreamView.Focus();
            }
        }

        private void exportToXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (exportToXmlFileDialog.ShowDialog() != DialogResult.OK)
                return;

            StringBuilder builder = new StringBuilder();

            builder.Append("<Streams>");

            foreach (VisualSession session in multiStreamView.Sessions)
            {
                builder.AppendFormat("<Stream LocalEndpoint=\"{0}\" RemoteEndpoint=\"{1}\">",
                    session.LocalEndpoint, session.RemoteEndpoint);

                foreach (VisualTransaction transaction in session.Transactions)
                {
                    builder.AppendFormat("<Transaction Index=\"{0}\" Direction=\"{1}\" StartTime=\"{2}\" EndTime=\"{3}\">",
                        transaction.Index,
                        (transaction.Direction == PacketDirection.PACKET_DIRECTION_INCOMING) ? "in" : "out",
                        transaction.StartTime.ToString("s"), transaction.EndTime.ToString("s"));

                    if (transaction.HeadlineText.Length > 0)
                    {
                        byte[] headlineBytes = StaticUtils.EncodeUTF8(transaction.HeadlineText);
                        builder.AppendFormat("<Headline>{0}</Headline>", Convert.ToBase64String(headlineBytes));
                    }

                    List<KeyValuePair<string, string>> headerFields = transaction.HeaderFields;
                    if (headerFields.Count > 0)
                    {
                        builder.Append("<Headers>");

                        foreach (KeyValuePair<string, string> pair in headerFields)
                        {
                            byte[] headerNameBytes = StaticUtils.EncodeUTF8(pair.Key);
                            byte[] headerValueBytes = StaticUtils.EncodeUTF8(pair.Value);

                            builder.AppendFormat("<Header Name=\"{0}\">{1}</Header>",
                                Convert.ToBase64String(headerNameBytes), Convert.ToBase64String(headerValueBytes));
                        }

                        builder.Append("</Headers>");
                    }

                    if (transaction.BodyText.Length > 0)
                    {
                        byte[] bodyBytes = StaticUtils.EncodeUTF8(transaction.BodyText);
                        builder.AppendFormat("<Body>{0}</Body>", Convert.ToBase64String(bodyBytes));
                    }

                    builder.Append("</Transaction>");
                }

                builder.AppendFormat("</Stream>");
            }

            builder.Append("</Streams>");

            FileStream fs = new FileStream(exportToXmlFileDialog.FileName, FileMode.Create);
            byte[] bytes = StaticUtils.EncodeUTF8(builder.ToString());
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();
        }
    }
}