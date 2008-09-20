/**
 * Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace oSpyClassic.Util
{
    class XmlUtils
    {
        public static void PrettyPrint(string xmlData, out string prettyXml)
        {
            PrettyPrint(xmlData, out prettyXml, null);
        }

        public static void PrettyPrint(string xmlData, out string prettyXml, XmlHighlighter highlighter)
        {
            StringReader stringReader = new StringReader(xmlData);
            XmlTextReader reader = new XmlTextReader(stringReader);
            StringBuilder builder = new StringBuilder(xmlData.Length);
            string[] tokens;
            char[] nsSepChars = new char[] { ':' };

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
                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_OPENING, offset, builder.Length - offset);

                        offset = builder.Length;
                        tokens = reader.Name.Split(nsSepChars, 2);
                        if (tokens.Length > 1)
                        {
                            builder.Append(tokens[0]);
                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_NAMESPACE_NAME, offset, builder.Length - offset);

                            offset = builder.Length;
                            builder.Append(":");
                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_NAMESPACE_SEPARATOR, offset, 1);

                            offset = builder.Length;
                            builder.Append(tokens[1]);
                        }
                        else
                        {
                            builder.Append(tokens[0]);
                        }

                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_NAME, offset, builder.Length - offset);

                        bool empty;
                        empty = reader.IsEmptyElement;

                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);

                            builder.Append(" ");

                            offset = builder.Length;

                            offset = builder.Length;
                            tokens = reader.Name.Split(nsSepChars, 2);
                            if (tokens.Length > 1)
                            {
                                builder.Append(tokens[0]);
                                if (highlighter != null)
                                    highlighter.AddToken(XmlHighlightContext.ELEMENT_ATTRIBUTE_KEY, offset, builder.Length - offset);

                                offset = builder.Length;
                                builder.Append(":");
                                if (highlighter != null)
                                    highlighter.AddToken(XmlHighlightContext.ELEMENT_NAMESPACE_SEPARATOR, offset, 1);

                                offset = builder.Length;
                                builder.Append(tokens[1]);
                            }
                            else
                            {
                                builder.Append(tokens[0]);
                            }

                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_ATTRIBUTE_KEY, offset, builder.Length - offset);

                            builder.Append("=\"");

                            offset = builder.Length;
                            builder.Append(reader.Value);
                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_ATTRIBUTE_VALUE, offset, builder.Length - offset);

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
                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_CLOSING, offset, builder.Length - offset);

                        builder.Append("\n");

                        break;
                    case XmlNodeType.EndElement:
                        indent -= 4;

                        builder.AppendFormat("{0,-" + Convert.ToString(indent) + "}", "");

                        offset = builder.Length;
                        builder.Append("</");
                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_OPENING, offset, builder.Length - offset);

                        offset = builder.Length;
                        tokens = reader.Name.Split(nsSepChars, 2);
                        if (tokens.Length > 1)
                        {
                            builder.Append(tokens[0]);
                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_NAMESPACE_NAME, offset, builder.Length - offset);

                            offset = builder.Length;
                            builder.Append(":");
                            if (highlighter != null)
                                highlighter.AddToken(XmlHighlightContext.ELEMENT_NAMESPACE_SEPARATOR, offset, 1);

                            offset = builder.Length;
                            builder.Append(tokens[1]);
                        }
                        else
                        {
                            builder.Append(tokens[0]);
                        }

                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_NAME, offset, builder.Length - offset);

                        offset = builder.Length;
                        builder.Append(">");
                        if (highlighter != null)
                            highlighter.AddToken(XmlHighlightContext.ELEMENT_CLOSING, offset, builder.Length - offset);

                        builder.Append("\n");
                        break;
                    case XmlNodeType.Text:
                        builder.AppendFormat("{0,-" + Convert.ToString(indent) + "}", "");
                        builder.Append(reader.Value);
                        builder.Append("\n");
                        break;
                }
            }

            prettyXml = builder.ToString().TrimEnd(new char[] { '\n' });
        }
    }

    public enum XmlHighlightContext
    {
        ELEMENT_OPENING,
        ELEMENT_CLOSING,
        ELEMENT_NAMESPACE_NAME,
        ELEMENT_NAMESPACE_SEPARATOR,
        ELEMENT_NAME,
        ELEMENT_ATTRIBUTE_KEY,
        ELEMENT_ATTRIBUTE_VALUE,
    }

    public class XmlHighlightColorScheme
    {
        protected Color elementOpeningColor;
        public Color ElementOpeningColor
        {
            get { return elementOpeningColor; }
            set { elementOpeningColor = value; }
        }

        protected Color elementClosingColor;
        public Color ElementClosingColor
        {
            get { return elementClosingColor; }
            set { elementClosingColor = value; }
        }

        protected Color elementNamespaceNameColor;
        public Color ElementNamespaceNameColor
        {
            get { return elementNamespaceNameColor; }
            set { elementNamespaceNameColor = value; }
        }

        protected Color elementNamespaceSeparatorColor;
        public Color ElementNamespaceSeparatorColor
        {
            get { return elementNamespaceSeparatorColor; }
            set { elementNamespaceSeparatorColor = value; }
        }

        protected Color elementNameColor;
        public Color ElementNameColor
        {
            get { return elementNameColor; }
            set { elementNameColor = value; }
        }

        protected Color attributeKeyColor;
        public Color AttributeKeyColor
        {
            get { return attributeKeyColor; }
            set { attributeKeyColor = value; }
        }

        protected Color attributeValueColor;
        public Color AttributeValueColor
        {
            get { return attributeValueColor; }
            set { attributeValueColor = value; }
        }

        public XmlHighlightColorScheme()
        {
            elementOpeningColor = Color.Black;
            elementClosingColor = Color.Black;
            elementNamespaceNameColor = Color.Black;
            elementNamespaceSeparatorColor = Color.Black;
            elementNameColor = Color.Black;
            attributeKeyColor = Color.Black;
            attributeValueColor = Color.Black;
        }

        static protected XmlHighlightColorScheme darkBlueScheme;
        static public XmlHighlightColorScheme DarkBlueScheme
        {
            get { return darkBlueScheme; }
        }

        static protected XmlHighlightColorScheme visualizationScheme;
        static public XmlHighlightColorScheme VisualizationScheme
        {
            get { return visualizationScheme; }
        }

        static XmlHighlightColorScheme()
        {
            Color cyanish = Color.FromArgb(64, 255, 255);
            Color darkCyanish = Color.FromArgb(0, 128, 128);
            Color greenish = Color.FromArgb(96, 255, 96);
            Color darkGreenish = Color.FromArgb(46, 139, 87);
            Color redish = Color.FromArgb(255, 160, 160);
            Color bluish = Color.FromArgb(106, 90, 205);
            Color lightBluish = Color.FromArgb(128, 160, 255);
            Color pinkish = Color.FromArgb(255, 0, 255);
            Color brownish = Color.FromArgb(255, 165, 0);

            darkBlueScheme = new XmlHighlightColorScheme();
            darkBlueScheme.ElementOpeningColor = cyanish;
            darkBlueScheme.ElementClosingColor = cyanish;
            darkBlueScheme.ElementNamespaceNameColor = brownish;
            darkBlueScheme.ElementNamespaceSeparatorColor = lightBluish;
            darkBlueScheme.ElementNameColor = cyanish;
            darkBlueScheme.AttributeKeyColor = greenish;
            darkBlueScheme.AttributeValueColor = redish;

            visualizationScheme = new XmlHighlightColorScheme();
            visualizationScheme.ElementOpeningColor = darkCyanish;
            visualizationScheme.ElementClosingColor = darkCyanish;
            visualizationScheme.ElementNamespaceNameColor = bluish;
            visualizationScheme.ElementNamespaceSeparatorColor = Color.Blue;
            visualizationScheme.ElementNameColor = darkCyanish;
            visualizationScheme.AttributeKeyColor = darkGreenish;
            visualizationScheme.AttributeValueColor = pinkish;
        }
    }

    public class XmlHighlighter
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

        protected Dictionary<XmlHighlightContext, HighlightContext> contexts;

        public XmlHighlighter(XmlHighlightColorScheme scheme)
        {
            contexts = new Dictionary<XmlHighlightContext, HighlightContext>(5);
            contexts.Add(XmlHighlightContext.ELEMENT_OPENING, new HighlightContext(scheme.ElementOpeningColor));
            contexts.Add(XmlHighlightContext.ELEMENT_CLOSING, new HighlightContext(scheme.ElementClosingColor));
            contexts.Add(XmlHighlightContext.ELEMENT_NAMESPACE_NAME, new HighlightContext(scheme.ElementNamespaceNameColor));
            contexts.Add(XmlHighlightContext.ELEMENT_NAMESPACE_SEPARATOR, new HighlightContext(scheme.ElementNamespaceSeparatorColor));
            contexts.Add(XmlHighlightContext.ELEMENT_NAME, new HighlightContext(scheme.ElementNameColor));
            contexts.Add(XmlHighlightContext.ELEMENT_ATTRIBUTE_KEY, new HighlightContext(scheme.AttributeKeyColor));
            contexts.Add(XmlHighlightContext.ELEMENT_ATTRIBUTE_VALUE, new HighlightContext(scheme.AttributeValueColor));
        }

        public void AddToken(XmlHighlightContext ctx, int offset, int length)
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
}
