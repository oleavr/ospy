//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Xml;
using System.IO;
using oSpy.Util;

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
            XmlHighlighter highlighter = new XmlHighlighter(XmlHighlightColorScheme.DarkBlueScheme);

            XmlUtils.PrettyPrint(xmlData, out prettyXml, highlighter);

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