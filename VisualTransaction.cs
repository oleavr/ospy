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
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Extended;
using System.Runtime.Serialization;

namespace oSpy
{    
    [Serializable()]
    public class VisualTransaction : UserControl, ISerializable, IComparable
    {
        private int index;
        public int Index
        {
            get { return index; }
        }

        private PacketDirection direction;
        public PacketDirection Direction
        {
            get { return direction; }
        }

        private DateTime startTime;
        public DateTime StartTime
        {
            get { return startTime; }
            set { startTime = value; Invalidate(); }
        }

        private DateTime endTime;
        public DateTime EndTime
        {
            get { return endTime; }
            set { endTime = value; Invalidate(); }
        }

        private Color headerBackColor;
        public Color HeaderBackColor
        {
            get { return headerBackColor; }
            set { headerBackColor = value; Invalidate(); }
        }

        private Color headerForeColor;
        public Color HeaderForeColor
        {
            get { return headerForeColor; }
            set { headerForeColor = value; Invalidate(); }
        }

        private Color bodyBackColor;
        public Color BodyBackColor
        {
            get { return bodyBackColor; }
            set { bodyBackColor = value; Invalidate(); }
        }

        private Color bodyForeColor;
        public Color BodyForeColor
        {
            get { return bodyForeColor; }
            set { bodyForeColor = value; Invalidate(); }
        }

        private Font headerFont;
        private Font headerFontBold;
        public Font HeaderFont
        {
            get { return headerFont; }
            set
            {
                headerFont = value;
                headerFontBold = new Font(value, FontStyle.Bold);
                Recalibrate();
            }
        }

        private Font bodyFont;
        public Font BodyFont
        {
            get { return bodyFont; }
            set { bodyFont = value; Recalibrate(); }
        }

        private int headerRowsPerCol;
        public int HeaderRowsPerCol
        {
            get { return headerRowsPerCol; }
            set { headerRowsPerCol = value; Recalibrate(); }
        }

        private int framePenWidth;
        public int FramePenWidth
        {
            get { return framePenWidth; }
            set { framePenWidth = value; Recalibrate(); }
        }

        private Color frameColor;
        public Color FrameColor
        {
            get { return frameColor; }
            set { frameColor = value; Invalidate(); }
        }

        protected List<KeyValuePair<string, string>> headerFields;

        private string bodyText;
        private string[] bodyLines;
        public string BodyText
        {
            get { return bodyText; }
            set
            {
                bodyText = value;

                if (value.Length > 0)
                    bodyLines = value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                else
                    bodyLines = new string[0];

                Recalibrate();
            }
        }

        private string contextID;
        public string ContextID
        {
            get { return contextID; }
            set { contextID = value; Invalidate(); }
        }

        protected Image previewImage;
        public Image PreviewImage
        {
            get { return previewImage; }
            set { previewImage = value; Recalibrate(); }
        }

        private int testScenario;
        public int TestScenario
        {
            get { return testScenario; }
            set { testScenario = value; SetTestScenario(value); }
        }

        public VisualTransaction()
        {
            Initialize(0, PacketDirection.PACKET_DIRECTION_INVALID, DateTime.Now, DateTime.Now);
        }

        public VisualTransaction(int index, DateTime startTime)
        {
            Initialize(index, PacketDirection.PACKET_DIRECTION_INVALID, startTime, startTime);
        }

        public VisualTransaction(int index, DateTime startTime, DateTime endTime)
        {
            Initialize(index, PacketDirection.PACKET_DIRECTION_INVALID, startTime, startTime);
        }

        public VisualTransaction(int index, PacketDirection direction)
        {
            Initialize(index, direction, DateTime.Now, DateTime.Now);
        }

        public VisualTransaction(int index, PacketDirection direction, DateTime startTime)
        {
            Initialize(index, direction, startTime, startTime);
        }

        public VisualTransaction(int index, PacketDirection direction, DateTime startTime, DateTime endTime)
        {
            Initialize(index, direction, startTime, endTime);
        }

        private bool initializing;
        private static TransactionColorPool colorPool = new TransactionColorPool();

        private void Initialize(int index, PacketDirection direction, DateTime startTime, DateTime endTime)
        {
            initializing = true;

            this.index = index;

            this.direction = direction;

            if (direction == PacketDirection.PACKET_DIRECTION_INCOMING)
            {
                HeaderBackColor = Color.FromArgb(240, 240, 255);
                FrameColor = Color.FromArgb(62, 72, 251);
            }
            else if (direction == PacketDirection.PACKET_DIRECTION_OUTGOING)
            {
                HeaderBackColor = Color.FromArgb(252, 250, 227);
                FrameColor = Color.FromArgb(250, 189, 35);
            }
            else
            {
                HeaderBackColor = Color.FromArgb(240, 240, 255);
                FrameColor = Color.FromArgb(62, 72, 251);
            }

            this.startTime = startTime;
            this.endTime = endTime;

            BodyBackColor = HeaderBackColor;
            HeaderForeColor = BodyForeColor = Color.Black;

            HeaderFont = new Font("Lucida Console", 8);
            BodyFont = new Font("Lucida Console", 8);

            headerRowsPerCol = 5;

            framePenWidth = 1;

            headerFields = new List<KeyValuePair<string, string>>();
            BodyText = "";
            testScenario = -1;

            contextID = null;

            previewImage = null;

            initializing = false;

            Recalibrate();
        }

        public VisualTransaction(SerializationInfo info, StreamingContext ctx)
        {
            initializing = true;

            index = info.GetInt32("index");

            direction = (PacketDirection)info.GetValue("direction", typeof(PacketDirection));

            startTime = (DateTime) info.GetValue("startTime", typeof(DateTime));
            endTime = (DateTime)info.GetValue("endTime", typeof(DateTime));

            headerBackColor = (Color) info.GetValue("headerBackColor", typeof(Color));
            headerForeColor = (Color) info.GetValue("headerForeColor", typeof(Color));
            bodyBackColor = (Color) info.GetValue("bodyBackColor", typeof(Color));
            bodyForeColor = (Color) info.GetValue("bodyForeColor", typeof(Color));

            HeaderFont = (Font) info.GetValue("HeaderFont", typeof(Font));
            bodyFont = (Font) info.GetValue("bodyFont", typeof(Font));

            headerRowsPerCol = info.GetInt32("headerRowsPerCol");

            framePenWidth = (int) info.GetValue("framePenWidth", typeof(int));
            frameColor = (Color) info.GetValue("frameColor", typeof(Color));

            headerFields = (List<KeyValuePair<string, string>>)
                info.GetValue("headerFields", typeof(List<KeyValuePair<string, string>>));
            BodyText = info.GetString("BodyText");

            contextID = info.GetString("contextID");

            previewImage = (Image) info.GetValue("previewImage", typeof(Image));

            TestScenario = info.GetInt32("TestScenario");

            initializing = false;

            Recalibrate();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            info.AddValue("index", index);

            info.AddValue("direction", direction);

            info.AddValue("startTime", startTime);
            info.AddValue("endTime", endTime);

            info.AddValue("headerBackColor", headerBackColor);
            info.AddValue("headerForeColor", headerForeColor);
            info.AddValue("bodyBackColor", bodyBackColor);
            info.AddValue("bodyForeColor", bodyForeColor);

            info.AddValue("HeaderFont", headerFont);
            info.AddValue("bodyFont", bodyFont);

            info.AddValue("headerRowsPerCol", headerRowsPerCol);

            info.AddValue("framePenWidth", framePenWidth);
            info.AddValue("frameColor", frameColor);

            info.AddValue("headerFields", headerFields);
            info.AddValue("BodyText", BodyText);

            info.AddValue("contextID", contextID);

            info.AddValue("previewImage", previewImage);

            info.AddValue("TestScenario", TestScenario);
        }

        public int CompareTo(Object obj)
        {
            VisualTransaction otherTransaction = obj as VisualTransaction;

            int result = StartTime.CompareTo(otherTransaction.StartTime);

            if (result != 0)
                return result;
            else
                return index.CompareTo(otherTransaction.index);
        }

        private void SetTestScenario(int scenario)
        {
            if (scenario == 1)
            {
                ClearHeaderFields();

                AddHeaderField("SessionID", 0);
                AddHeaderField("MessageID", 184292);
                AddHeaderField("DataOffset", 0);
                AddHeaderField("DataSize", 713);
                AddHeaderField("ChunkSize", 713);
                AddHeaderField("Flags", 0);
                AddHeaderField("AckedMsgID", 43986843);
                AddHeaderField("PrevAckedMsgID", 0);
                AddHeaderField("AckedDataSize", 0);

                BodyText = "0000: 00 00 00 00 d5 fb 01 00 00 00 00 00 00 00 00 00  ................\r\n" +
                           "0010: 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00  ................\r\n" +
                           "0020: df 78 b2 85 a0 a8 d5 4c 92 a0 d7 f9 0c b7 a2 21  .x.....L.......!";
            }
        }

        public void ClearHeaderFields()
        {
            headerFields.Clear();
            Recalibrate();
        }

        public void AddHeaderField(string name, string value)
        {
            headerFields.Add(new KeyValuePair<string, string>(name, value));
            Recalibrate();
        }

        public void AddHeaderField(string name, PacketDirection direction)
        {
            AddHeaderField(name, (direction == PacketDirection.PACKET_DIRECTION_INCOMING) ? "in" : "out");
        }

        public void AddHeaderField(string name, object value)
        {
            AddHeaderField(name, Convert.ToString(value));
        }

        private int sectionSpacing = 5;

        private int ctxSquareWidth = 10;

        private int headerTopBottomSpacing = 5;
        private int headerLeftRightSpacing = 5;
        private int headerRowSpacing = 2;
        private int bodyBorderSpacing = 5;
        private int bodyLineSpacing = 2;

        private int commonWidth;
        private int headerX, headerY, headerContentX, headerContentY, headerContentWidth, headerRowHeight, headerHeight;
        private float headerWidestName;
        private float[] headerColWidths = new float[1];
        private int bodyX, bodyY, bodyContentX, bodyContentY, bodyLineHeight, bodyHeight;

        private void Recalibrate()
        {
            if (initializing)
                return;

            Graphics g = this.CreateGraphics();

            commonWidth = (int)g.MeasureString("0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................", bodyFont).Width;
            commonWidth += 2 * bodyBorderSpacing;

            //
            // Header
            //
            headerX = 0 + framePenWidth;
            headerY = 0 + framePenWidth;

            int headerFontHeight = (int)headerFont.GetHeight();

            headerContentX = headerX + headerLeftRightSpacing;
            headerContentY = headerY + headerTopBottomSpacing;

            headerContentWidth = commonWidth - (2 * headerLeftRightSpacing);

            headerRowHeight = headerFontHeight + headerRowSpacing;

            headerHeight = (headerFields.Count > 0) ? headerTopBottomSpacing * 2 : 0;
            if (headerFields.Count >= 1)
            {
                int tmp = headerFields.Count;
                if (tmp > headerRowsPerCol)
                    tmp = headerRowsPerCol;
                headerHeight += (headerRowHeight * tmp) - headerRowSpacing;
            }

            headerWidestName = 0;

            int n = headerFields.Count / headerRowsPerCol;
            if (headerFields.Count % headerRowsPerCol != 0)
                n++;

            headerColWidths = new float[n];

            for (int i = 0; i < headerFields.Count; i++)
            {
                KeyValuePair<string, string> field = headerFields[i];
                float fieldWidth = g.MeasureString(field.Key + ":", headerFontBold).Width;

                if (fieldWidth > headerWidestName)
                    headerWidestName = fieldWidth;

                int colNo = i / headerRowsPerCol;
                if (fieldWidth > headerColWidths[colNo])
                    headerColWidths[colNo] = fieldWidth;
            }

            //
            // Body
            //
            bodyX = headerX;
            bodyY = headerY + headerHeight + ((headerHeight > 0) ? sectionSpacing : 0);

            int bodyFontHeight = (int)bodyFont.GetHeight();

            bodyLineHeight = bodyFontHeight + bodyLineSpacing;

            bodyContentX = bodyX + bodyBorderSpacing;
            bodyContentY = bodyY + bodyBorderSpacing;

            bodyHeight = (2 * bodyBorderSpacing);
            if (bodyLines.Length >= 1)
            {
                bodyHeight += (bodyLineHeight * bodyLines.Length) - bodyLineSpacing;
            }

            if (previewImage != null)
            {
                bodyHeight += (2 * bodyLineHeight) + previewImage.Height;
            }

            UpdateSize();

            //
            // Redraw
            //
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            UpdateSize();
        }

        private void UpdateSize()
        {
            Width = commonWidth + (2 * framePenWidth);

            int height = headerHeight + (2 * framePenWidth);
            if (bodyLines.Length > 0)
            {
                height += ((headerHeight > 0) ? sectionSpacing : 0) + bodyHeight;
            }

            Height = height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ExtendedGraphics eg = new ExtendedGraphics(g);
            Rectangle rect = e.ClipRectangle;

            float x = 0, y = 0;
            Brush brush;
            Pen pen = new Pen(frameColor, framePenWidth);

            //
            // Header
            //
            if (headerFields.Count > 0)
            {
                eg.FillRoundRectangle(new SolidBrush(headerBackColor),
                    headerX, headerY, commonWidth, headerHeight, 2.0f);

                x = headerContentX;
                y = headerContentY;
                brush = new SolidBrush(headerForeColor);

                if (contextID != null)
                {
                    int ctxSquareX = headerContentX + headerContentWidth - 2 - ctxSquareWidth;
                    int ctxSquareY = headerContentY + 2;

                    Color c1, c2;
                    colorPool.GetColorsForId(contextID, out c1, out c2);

                    eg.DrawRoundRectangle(new Pen(c2, framePenWidth),
                        ctxSquareX, ctxSquareY,
                        ctxSquareWidth, ctxSquareWidth, 2);

                    g.FillRectangle(new SolidBrush(c1),
                        ctxSquareX + framePenWidth, ctxSquareY + framePenWidth,
                        ctxSquareWidth - framePenWidth,
                        ctxSquareWidth - framePenWidth);
                }

                for (int i = 0; i < headerFields.Count; i++)
                {
                    int colNo = i / headerRowsPerCol;
                    float fieldWidth = headerColWidths[colNo];

                    g.DrawString(headerFields[i].Key + ":", headerFontBold, brush, x, y);
                    g.DrawString(headerFields[i].Value, headerFont, brush, x + fieldWidth, y);

                    y += headerRowHeight;

                    if (((i + 1) % headerRowsPerCol) == 0)
                    {
                        x += headerContentWidth / headerColWidths.Length;
                        y = headerContentY;
                    }
                }
            }

            //
            // Body
            //
            if (bodyLines.Length > 0)
            {
                eg.FillRoundRectangle(new SolidBrush(bodyBackColor),
                    bodyX, bodyY, commonWidth, bodyHeight, 2.0f);

                brush = new SolidBrush(bodyForeColor);
                y = bodyContentY;
                foreach (string line in bodyLines)
                {
                    g.DrawString(line, bodyFont, brush, bodyContentX, y);

                    y += bodyLineHeight;
                }
            }

            if (previewImage != null)
            {
                y += bodyLineHeight;

                g.DrawImage(previewImage, bodyX + bodyLineHeight, y);

                y += bodyLineHeight;
            }

            //
            // Draw a round frame around both header and body
            //
            int width = commonWidth + framePenWidth;
            int height = headerHeight + framePenWidth;
            if (bodyLines.Length > 0)
            {
                height += ((headerHeight > 0) ? sectionSpacing : 0) + bodyHeight;
            }

            eg.DrawRoundRectangle(pen, headerX - framePenWidth, headerY - framePenWidth,
                                  width, height, 4.0f);
        }

        public void SetBodyFromPreviewData(byte[] data, int maxSize)
        {
            int lineCount;

            BodyText = Util.ByteArrayToHexDump(data, maxSize, "", out lineCount);
        }

        public void SetBodyFromTruncatedPreviewData(byte[] data, int bytesRemaining)
        {
            int lineCount;

            BodyText = Util.ByteArrayToHexDump(data, -1, "", out lineCount, bytesRemaining);
        }

        public virtual void TransactionsCreated()
        {
        }

        public virtual void SessionsCreated()
        {
        }
    }

    public class TransactionColorPool
    {
        private static List<KeyValuePair<Color, Color>> colorPairs;
        private Dictionary<string, KeyValuePair<Color,Color>> usedColorPairs;
        private int index;

        static TransactionColorPool()
        {
            colorPairs = new List<KeyValuePair<Color, Color>>();

            AddColorPair("#23379a", "#1b76d3"); // bluish
            AddColorPair("#24a338", "#8ad530"); // greenish
            AddColorPair("#fc7f1c", "#fec623"); // orangish
            AddColorPair("#5c1e7e", "#d42279"); // magentish
            // FIXME: add more colorpairs
        }

        private static void AddColorPair(string color1, string color2)
        {
            Color c1 = ColorFromHexString(color1);
            Color c2 = ColorFromHexString(color2);
            colorPairs.Add(new KeyValuePair<Color, Color>(c1, c2));
        }

        private static Color ColorFromHexString(string str)
        {
            return Color.FromArgb(
                Convert.ToByte(str.Substring(1, 2), 16),
                Convert.ToByte(str.Substring(3, 2), 16),
                Convert.ToByte(str.Substring(5, 2), 16));
        }

        public TransactionColorPool()
        {
            index = 0;
            usedColorPairs = new Dictionary<string, KeyValuePair<Color, Color>>();
        }

        public void GetColorsForId(string id, out Color c1, out Color c2)
        {
            if (usedColorPairs.ContainsKey(id))
            {
                c1 = usedColorPairs[id].Key;
                c2 = usedColorPairs[id].Value;
                return;
            }

            int wrapCount = index / colorPairs.Count;
            int colIndex = index % colorPairs.Count;

            c1 = MakeColor(colorPairs[colIndex].Key, wrapCount);
            c2 = MakeColor(colorPairs[colIndex].Value, wrapCount);

            usedColorPairs[id] = new KeyValuePair<Color,Color>(c1, c2);
            index++;
        }

        private Color MakeColor(Color baseColor, int wrapCount)
        {
            return Color.FromArgb((baseColor.R + (10 * wrapCount)) % 256,
                                  (baseColor.G + (10 * wrapCount)) % 256,
                                  (baseColor.B + (10 * wrapCount)) % 256);

        }
    }
}
