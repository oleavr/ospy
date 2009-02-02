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
using System.Xml;

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

            exportToXMLToolStripMenuItem.Enabled = false;
            submitToRepositoryToolStripMenuItem.Enabled = false;
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

                exportToXMLToolStripMenuItem.Enabled = (visSessions.Count > 0);
                submitToRepositoryToolStripMenuItem.Enabled = (visSessions.Count > 0);
            }
        }

        private void exportToXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (exportToXmlFileDialog.ShowDialog() != DialogResult.OK)
                return;

            XmlDocument doc = ExportToXml();
            doc.Save(exportToXmlFileDialog.FileName);
        }

        private void submitToRepositoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShareVisualizationForm frm = new ShareVisualizationForm(this);
            frm.ShowDialog(this);
        }

        public XmlDocument ExportToXml()
        {
            XmlDocument doc = new XmlDocument();

            XmlElement streamsElement = doc.CreateElement("Streams");
            doc.AppendChild(streamsElement);

            foreach (VisualSession session in multiStreamView.Sessions)
            {
                XmlElement streamElement = doc.CreateElement("Stream");
                streamElement.SetAttribute("LocalEndpoint", session.LocalEndpoint.ToString());
                streamElement.SetAttribute("RemoteEndpoint", session.RemoteEndpoint.ToString());
                streamsElement.AppendChild(streamElement);

                foreach (VisualTransaction transaction in session.Transactions)
                {
                    XmlElement transactionElement = doc.CreateElement("Transaction");
                    transactionElement.SetAttribute("Index", Convert.ToString(transaction.Index));
                    transactionElement.SetAttribute("Direction", (transaction.Direction == PacketDirection.PACKET_DIRECTION_INCOMING) ? "in" : "out");
                    transactionElement.SetAttribute("StartTime", transaction.StartTime.ToString("s"));
                    transactionElement.SetAttribute("EndTime", transaction.EndTime.ToString("s"));
                    streamElement.AppendChild(transactionElement);

                    if (transaction.HeadlineText.Length > 0)
                    {
                        XmlElement headlineElement = doc.CreateElement("Headline");
                        headlineElement.AppendChild(doc.CreateTextNode(transaction.HeadlineText));
                        transactionElement.AppendChild(headlineElement);
                    }

                    List<KeyValuePair<string, string>> headerFields = transaction.HeaderFields;
                    if (headerFields.Count > 0)
                    {
                        XmlElement headersElement = doc.CreateElement("Headers");
                        transactionElement.AppendChild(headersElement);

                        foreach (KeyValuePair<string, string> pair in headerFields)
                        {
                            XmlElement headerElement = doc.CreateElement("Header");
                            headerElement.SetAttribute("Name", pair.Key.Substring(0, pair.Key.Length - 1));
                            headerElement.AppendChild(doc.CreateTextNode(pair.Value));
                            headersElement.AppendChild(headerElement);
                        }
                    }

                    if (transaction.BodyText.Length > 0)
                    {
                        XmlElement bodyElement = doc.CreateElement("Body");
                        bodyElement.AppendChild(doc.CreateTextNode(transaction.BodyText));
                        transactionElement.AppendChild(bodyElement);
                    }
                }
            }

            return doc;
        }
    }
}