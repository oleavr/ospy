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
            StringReader stringReader = new StringReader(xmlData);
            XmlTextReader reader = new XmlTextReader(stringReader);
            StringBuilder builder = new StringBuilder(xmlData.Length);

            builder.Append("FORMATTED:\n----------\n\n");

            XMLHighlighter highlighter = new XMLHighlighter();

            int indent = 0;
            while (reader.Read())
            {
                int offset;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        builder.AppendFormat("{0,-" + Convert.ToString(indent) + "}", "");

                        offset = builder.Length;
                        builder.Append("<");
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_OPENING, offset, builder.Length - offset);

                        offset = builder.Length;
                        builder.Append(reader.Name);
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_NAME, offset, builder.Length - offset);
                        
                        bool empty;
                        empty = reader.IsEmptyElement;

                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);

                            builder.Append(" ");

                            offset = builder.Length;
                            builder.Append(reader.Name);
                            highlighter.AddToken(XMLHighlightContext.ELEMENT_ATTRIBUTE_KEY, offset, builder.Length - offset);

                            builder.Append("=\"");

                            offset = builder.Length;
                            builder.Append(reader.Value);
                            highlighter.AddToken(XMLHighlightContext.ELEMENT_ATTRIBUTE_VALUE, offset, builder.Length - offset);

                            builder.Append("\"");
                        }

                        offset = builder.Length;

                        if (empty)
                        {
                            builder.Append("/");
                        }
                        else
                        {
                            indent += 4;
                        }

                        builder.Append(">");
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_CLOSING, offset, builder.Length - offset);

                        builder.Append("\n");

                        break;
                    case XmlNodeType.EndElement:
                        indent -= 4;

                        builder.AppendFormat("{0,-" + Convert.ToString(indent) + "}", "");

                        offset = builder.Length;
                        builder.Append("</");
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_OPENING, offset, builder.Length - offset);

                        offset = builder.Length;
                        builder.Append(reader.Name);
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_NAME, offset, builder.Length - offset);

                        offset = builder.Length;
                        builder.Append(">");
                        highlighter.AddToken(XMLHighlightContext.ELEMENT_CLOSING, offset, builder.Length - offset);

                        builder.Append("\n");
                        break;
                    case XmlNodeType.Text:
                        builder.AppendFormat("{0,-" + Convert.ToString(indent) + "}", "");
                        builder.Append(reader.Value);
                        builder.Append("\n");
                        break;
                }
            }

            builder.AppendFormat("\n\n\nRAW:\n----\n\n{0}", xmlData);

            richTextBox.Text = builder.ToString();

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

    public enum XMLHighlightContext
    {
        ELEMENT_OPENING,
        ELEMENT_CLOSING,
        ELEMENT_NAME,
        ELEMENT_ATTRIBUTE_KEY,
        ELEMENT_ATTRIBUTE_VALUE,
    };

    public class XMLHighlighter
    {
        protected class HighlightContext
        {
            protected Color color;
            public Color Color
            {
                get
                {
                    return color;
                }
            }

            protected List<KeyValuePair<int, int>> tags;
            public KeyValuePair<int, int>[] Tags
            {
                get
                {
                    return tags.ToArray();
                }
            }

            public HighlightContext(Color c)
            {
                color = c;
                tags = new List<KeyValuePair<int, int>>();
            }

            public void Add(int offset, int length)
            {
                tags.Add(new KeyValuePair<int, int>(offset, length));
            }
        }

        protected Dictionary<XMLHighlightContext, HighlightContext> contexts;

        public XMLHighlighter()
        {
            Color cyanish = Color.FromArgb(64, 255, 255);
            Color greenish = Color.FromArgb(96, 255, 96);
            Color redish = Color.FromArgb(255, 160, 160);

            contexts = new Dictionary<XMLHighlightContext, HighlightContext>(5);
            contexts.Add(XMLHighlightContext.ELEMENT_OPENING, new HighlightContext(cyanish));
            contexts.Add(XMLHighlightContext.ELEMENT_CLOSING, new HighlightContext(cyanish));
            contexts.Add(XMLHighlightContext.ELEMENT_NAME, new HighlightContext(cyanish));
            contexts.Add(XMLHighlightContext.ELEMENT_ATTRIBUTE_KEY, new HighlightContext(greenish));
            contexts.Add(XMLHighlightContext.ELEMENT_ATTRIBUTE_VALUE, new HighlightContext(redish));
        }

        public void AddToken(XMLHighlightContext ctx, int offset, int length)
        {
            contexts[ctx].Add(offset, length);
        }

        public void HighlightRichTextBox(RichTextBox box)
        {
            foreach (HighlightContext ctx in contexts.Values)
            {
                foreach (KeyValuePair<int, int> pair in ctx.Tags)
                {
                    box.Select(pair.Key, pair.Value);
                    box.SelectionColor = ctx.Color;
                }
            }

            box.Select(0, 0);
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