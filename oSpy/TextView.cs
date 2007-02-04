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
using System.Xml;
using System.IO;

namespace oSpy
{
    public partial class TextView : Form
    {
        public TextView()
        {
            InitializeComponent();
        }

        public void DisplayText(string text)
        {
            richTextBox.Text = text;
        }

        public void DisplayXML(string xmlData)
        {
            string prettyXml;
            Util.XMLHighlighter highlighter;

            Util.XML.PrettyPrint(xmlData, out prettyXml, out highlighter);

            richTextBox.Text = prettyXml;
            highlighter.HighlightRichTextBox(richTextBox);
        }

        private void HighlightList(List<KeyValuePair<int, int>> tags, Color color)
        {
            foreach (KeyValuePair<int, int> el in tags)
            {
                richTextBox.Select(el.Key, el.Value);
                richTextBox.SelectionBackColor = color;
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    public class TextUITypeEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            TextView view = new TextView();
            view.DisplayText(value as string);
            view.ShowDialog();

            return value;
        }
    }

    public class XMLUITypeEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            TextView view = new TextView();
            view.DisplayXML(value as string);
            view.ShowDialog();

            return value;
        }
    }
}