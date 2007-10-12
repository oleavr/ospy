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
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Extended;
using System.Runtime.Serialization;
using oSpy.Util;
using System.Runtime.InteropServices;

namespace oSpy
{
    [Serializable()]
    public class VisualTransaction : UserControl, ISerializable, IComparable, IDrawToBitmapFull
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

        public Color HeadlineBackColor
        {
            get { return headlineBox.BackColor; }
            set { headlineBox.BackColor = value; Invalidate(); }
        }

        public Color HeadlineForeColor
        {
            get { return headlineBox.ForeColor; }
            set { headlineBox.ForeColor = value; }
        }

        private Color headerBackColor;
        public Color HeaderBackColor
        {
            get { return headerBackColor; }
            set { headerBackColor = value; UpdateHeaders(false); Invalidate(); }
        }

        private Color headerForeColor;
        public Color HeaderForeColor
        {
            get { return headerForeColor; }
            set { headerForeColor = value; UpdateHeaders(false); }
        }

        public Color BodyBackColor
        {
            get { return bodyBox.BackColor; }
            set { bodyBox.BackColor = value; Invalidate(); }
        }

        public Color BodyForeColor
        {
            get { return bodyBox.ForeColor; }
            set { bodyBox.ForeColor = value; }
        }

        public Font HeadlineFont
        {
            get { return headlineBox.Font; }
            set { headlineBox.Font = value; Recalibrate(); }
        }

        private Font headerFont, headerFontBold;
        public Font HeaderFont
        {
            get { return headerFont; }
            set
            {
                headerFont = value;
                headerFontBold = new Font(headerFont, FontStyle.Bold);
                UpdateHeaders(true);
            }
        }

        public Font BodyFont
        {
            get { return bodyBox.Font; }
            set { bodyBox.Font = value; Recalibrate(); }
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

        protected GroovyRichTextBox headlineBox;
        public GroovyRichTextBox HeadlineBox
        {
            get { return headlineBox; }
        }

        public string HeadlineText
        {
            get { return headlineBox.Text; }
            set { headlineBox.Text = value; Recalibrate(); }
        }

        protected List<KeyValuePair<TextBox, TextBox>> headerBoxes;

        public List<KeyValuePair<string, string>> HeaderFields
        {
            get
            {
                List<KeyValuePair<string, string>> fields = new List<KeyValuePair<string, string>>(headerBoxes.Count);

                foreach (KeyValuePair<TextBox, TextBox> boxPair in headerBoxes)
                {
                    fields.Add(new KeyValuePair<string, string>(boxPair.Key.Text, boxPair.Value.Text));
                }

                return fields;
            }
        }

        protected GroovyRichTextBox bodyBox;
        public GroovyRichTextBox BodyBox
        {
            get { return bodyBox; }
        }

        public string BodyText
        {
            get { return bodyBox.Text; }
            set
            {
                bodyBox.Text = value;

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

        private void InitializeBasics()
        {
            headlineBox = new GroovyRichTextBox();
            headlineBox.Parent = this;

            headerBoxes = new List<KeyValuePair<TextBox, TextBox>>();

            bodyBox = new GroovyRichTextBox();
            bodyBox.Parent = this;
        }

        private void Initialize(int index, PacketDirection direction, DateTime startTime, DateTime endTime)
        {
            initializing = true;

            InitializeBasics();

            this.index = index;

            this.direction = direction;

            if (direction == PacketDirection.PACKET_DIRECTION_INCOMING)
            {
                headerBackColor = Color.FromArgb(240, 240, 255);
                FrameColor = Color.FromArgb(62, 72, 251);
            }
            else if (direction == PacketDirection.PACKET_DIRECTION_OUTGOING)
            {
                headerBackColor = Color.FromArgb(252, 250, 227);
                FrameColor = Color.FromArgb(250, 189, 35);
            }
            else
            {
                headerBackColor = Color.FromArgb(240, 240, 255);
                FrameColor = Color.FromArgb(62, 72, 251);
            }

            this.startTime = startTime;
            this.endTime = endTime;

            HeadlineBackColor = BodyBackColor = HeaderBackColor;
            HeadlineForeColor = headerForeColor = BodyForeColor = Color.Black;

            HeadlineFont = new Font("Lucida Console", 8);
            HeaderFont = new Font("Lucida Console", 8);
            BodyFont = new Font("Lucida Console", 8);

            headerRowsPerCol = 5;

            framePenWidth = 1;

            testScenario = -1;

            contextID = null;

            previewImage = null;

            initializing = false;

            Recalibrate();
        }

        public VisualTransaction(SerializationInfo info, StreamingContext ctx)
        {
            initializing = true;

            InitializeBasics();

            index = info.GetInt32("index");

            direction = (PacketDirection)info.GetValue("direction", typeof(PacketDirection));

            startTime = (DateTime) info.GetValue("startTime", typeof(DateTime));
            endTime = (DateTime)info.GetValue("endTime", typeof(DateTime));

            HeadlineBackColor = (Color)info.GetValue("HeadlineBackColor", typeof(Color));
            HeadlineForeColor = (Color)info.GetValue("HeadlineForeColor", typeof(Color));

            headerBackColor = (Color)info.GetValue("headerBackColor", typeof(Color));
            headerForeColor = (Color) info.GetValue("headerForeColor", typeof(Color));

            BodyBackColor = (Color)info.GetValue("BodyBackColor", typeof(Color));
            BodyForeColor = (Color)info.GetValue("BodyForeColor", typeof(Color));

            HeadlineFont = (Font)info.GetValue("HeadlineFont", typeof(Font));

            HeaderFont = (Font)info.GetValue("HeaderFont", typeof(Font));

            BodyFont = (Font)info.GetValue("BodyFont", typeof(Font));

            headerRowsPerCol = info.GetInt32("headerRowsPerCol");

            framePenWidth = (int) info.GetValue("framePenWidth", typeof(int));
            frameColor = (Color) info.GetValue("frameColor", typeof(Color));

            HeadlineText = info.GetString("HeadlineText");

            bodyBox.Rtf = info.GetString("BodyRtf");

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

            info.AddValue("HeadlineBackColor", HeadlineBackColor);
            info.AddValue("HeadlineForeColor", HeadlineForeColor);

            info.AddValue("headerBackColor", headerBackColor);
            info.AddValue("headerForeColor", headerForeColor);

            info.AddValue("BodyBackColor", BodyBackColor);
            info.AddValue("BodyForeColor", BodyForeColor);

            info.AddValue("HeadlineFont", HeadlineFont);

            info.AddValue("HeaderFont", headerFont);

            info.AddValue("BodyFont", BodyFont);

            info.AddValue("headerRowsPerCol", headerRowsPerCol);

            info.AddValue("framePenWidth", framePenWidth);
            info.AddValue("frameColor", frameColor);

            info.AddValue("HeadlineText", HeadlineText);

            info.AddValue("BodyRtf", bodyBox.Rtf);

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
                SetExampleHeadline();
                ClearHeaderFields();
                BodyText = "";
            }
            else if (scenario == 2)
            {
                HeadlineText = "";
                SetExampleHeaders();
                BodyText = "";
            }
            else if (scenario == 3)
            {
                HeadlineText = "";
                ClearHeaderFields();
                SetExampleBody();
            }
            else if (scenario == 4)
            {
                SetExampleHeadline();
                SetExampleHeaders();
                BodyText = "";
            }
            else if (scenario == 5)
            {
                SetExampleHeadline();
                ClearHeaderFields();
                SetExampleBody();
            }
            else if (scenario == 6)
            {
                SetExampleHeadline();
                SetExampleHeaders();
                SetExampleBody();
            }
            else
            {
                HeadlineText = "";
                ClearHeaderFields();
                BodyText = "";
            }
        }

        private void SetExampleHeadline()
        {
            HeadlineText = "Woot baby";
        }

        private void SetExampleHeaders()
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
            AddHeaderField("SoDamnLong", "ABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABCABC");
        }

        private void SetExampleBody()
        {
            BodyText = "0000: 00 00 00 00 d5 fb 01 00 00 00 00 00 00 00 00 00  ................\r\n" +
                       "0010: 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00  ................\r\n" +
                       "0020: df 78 b2 85 a0 a8 d5 4c 92 a0 d7 f9 0c b7 a2 21  .x.....L.......!";
        }

        public void ClearHeaderFields()
        {
            foreach (KeyValuePair<TextBox,TextBox> pair in headerBoxes)
            {
                pair.Key.Parent = null;
                pair.Value.Parent = null;
            }

            headerBoxes.Clear();

            Recalibrate();
        }

        public void AddHeaderField(string name, string value)
        {
            KeyValuePair<TextBox, TextBox> field = new KeyValuePair<TextBox, TextBox>(new TextBox(), new TextBox());

            field.Key.Text = name + ":";
            ApplyHeaderBoxStyle(field.Key, true);

            field.Value.Text = value;
            ApplyHeaderBoxStyle(field.Value, false);

            headerBoxes.Add(field);

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

        private void UpdateHeaders(bool recalibrate)
        {
            foreach (KeyValuePair<TextBox, TextBox> field in headerBoxes)
            {
                ApplyHeaderBoxStyle(field.Key, true);
                ApplyHeaderBoxStyle(field.Value, false);
            }

            if (recalibrate)
                Recalibrate();
        }

        private void ApplyHeaderBoxStyle(TextBox box, bool isKey)
        {
            box.Font = (isKey) ? headerFontBold : headerFont;
            box.ReadOnly = true;
            box.BorderStyle = BorderStyle.None;
            box.ForeColor = headerForeColor;
            box.BackColor = headerBackColor;
        }

        private const int sectionSpacing = 5;

        private const int ctxSquareWidth = 10;

        private const int boxesTopBottomSpacing = 5;
        private const int boxesLeftRightSpacing = 5;

        private const int headlineMaxHeight = 30;
        private const int headerTopBottomSpacing = 5;
        private const int headerLeftRightSpacing = 7;
        private const int headerRowSpacing = 2;
        private const int bodyBoxBorderWidth = 28;
        private const int bodyMaxHeight = 200;

        private int commonWidth;
        private int headerX, headerY, headerContentX, headerContentY, headerContentWidth, headerRowHeight, headerHeight;
        private float headerWidestName;

        private void Recalibrate()
        {
            if (initializing)
                return;

            Graphics g = this.CreateGraphics();

            commonWidth = (int)g.MeasureString("0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00  ................", bodyBox.Font).Width;
            commonWidth += 2 * bodyBoxBorderWidth;

            //
            // Headline
            //
            headlineBox.Left = 0 + framePenWidth + boxesLeftRightSpacing;
            headlineBox.Top = 0 + framePenWidth + boxesTopBottomSpacing;
            headlineBox.Width = commonWidth - (2 * boxesLeftRightSpacing);
            int height = headlineBox.GetPreferredSize(new Size(commonWidth, headlineMaxHeight)).Height;
            if (height > headlineMaxHeight)
                height = headlineMaxHeight;
            headlineBox.Height = height;
            headlineBox.Visible = (headlineBox.Text.Length > 0);

            //
            // Header
            //
            headerX = 0 + framePenWidth;
            headerY = 0;
            if (headlineBox.Visible)
                headerY += headlineBox.Top + headlineBox.Height + boxesTopBottomSpacing + sectionSpacing;
            else
                headerY += framePenWidth;

            headerContentX = headerX + headerLeftRightSpacing;
            headerContentY = headerY + headerTopBottomSpacing;

            headerContentWidth = commonWidth - (2 * headerLeftRightSpacing);

            if (headerBoxes.Count > 0)
                headerRowHeight = Math.Max(headerBoxes[0].Key.Height, headerBoxes[0].Value.Height) + headerRowSpacing;
            else
                headerRowHeight = 0;

            headerHeight = (headerBoxes.Count > 0) ? headerTopBottomSpacing * 2 : 0;
            if (headerBoxes.Count > 0)
            {
                int tmp = headerBoxes.Count;
                if (tmp > headerRowsPerCol)
                    tmp = headerRowsPerCol;
                headerHeight += (headerRowHeight * tmp) - headerRowSpacing;
            }

            headerWidestName = 0;

            int n = headerBoxes.Count / headerRowsPerCol;
            if (headerBoxes.Count % headerRowsPerCol != 0)
                n++;

            int[] headerKeyWidths = new int[n];
            Size proposedSize = new Size(1, 1);

            for (int i = 0; i < headerBoxes.Count; i++)
            {
                KeyValuePair<TextBox, TextBox> field = headerBoxes[i];
                int colNo = i / headerRowsPerCol;

                field.Key.Size = field.Key.GetPreferredSize(proposedSize);

                if (field.Key.Width > headerWidestName)
                    headerWidestName = field.Key.Width;

                if (field.Key.Width > headerKeyWidths[colNo])
                    headerKeyWidths[colNo] = field.Key.Width;
            }

            int x = headerContentX;
            int y = headerContentY;
            int headerColWidth = headerContentWidth;
            if (headerKeyWidths.Length > 0)
                headerColWidth /= headerKeyWidths.Length;

            for (int i = 0; i < headerBoxes.Count; i++)
            {
                KeyValuePair<TextBox, TextBox> field = headerBoxes[i];
                int colNo = i / headerRowsPerCol;

                field.Key.Parent = this;
                field.Key.Left = x;
                field.Key.Top = y;
                field.Key.Width = headerKeyWidths[colNo];

                field.Value.Parent = this;
                field.Value.Left = x + headerKeyWidths[colNo];
                field.Value.Top = y;
                field.Value.Width = headerColWidth - field.Key.Width;
                field.Value.Height = field.Key.Height;

                y += headerRowHeight;

                if (((i + 1) % headerRowsPerCol) == 0)
                {
                    x += headerColWidth;
                    y = headerContentY;
                }
            }

            //
            // Body
            //
            bodyBox.Left = headerX + boxesLeftRightSpacing;
            bodyBox.Top = headerY + headerHeight + ((headerHeight > 0) ? sectionSpacing : 0) + boxesTopBottomSpacing;
            bodyBox.Width = commonWidth - (2 * boxesLeftRightSpacing);
            height = bodyBox.GetPreferredSize(new Size(commonWidth, bodyMaxHeight)).Height;
            if (height > bodyMaxHeight)
                height = bodyMaxHeight;
            bodyBox.Height = height;
            bodyBox.Visible = (bodyBox.Text.Length > 0);

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

            int height = 2 * framePenWidth;

            if (headlineBox.Visible)
            {
                height += headlineBox.Height + (2 * boxesTopBottomSpacing);
            }

            height += headerHeight;

            if (bodyBox.Visible)
            {
                height += bodyBox.Height + (2 * boxesTopBottomSpacing);
            }
            
            int count = 0;
            if (headlineBox.Visible)  count++;
            if (headerHeight != 0)  count++;
            if (bodyBox.Visible)  count++;

            if (count > 0)
                height += sectionSpacing * (count - 1);

            Height = height;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            Graphics g = e.Graphics;
            Brush brush;

            //
            // Headline background
            //
            if (headlineBox.Visible)
            {
                brush = new SolidBrush(headlineBox.BackColor);

                g.FillRectangle(brush,
                    headlineBox.Left - boxesLeftRightSpacing,
                    headlineBox.Top - boxesTopBottomSpacing,
                    commonWidth, (2 * boxesTopBottomSpacing) + headlineBox.Height);
            }

            //
            // Headers background
            //
            if (headerBoxes.Count > 0)
            {
                brush = new SolidBrush(headerBackColor);

                g.FillRectangle(brush, headerX, headerY, commonWidth, headerHeight);
            }

            //
            // Body background
            //
            if (bodyBox.Visible)
            {
                brush = new SolidBrush(bodyBox.BackColor);

                g.FillRectangle(brush,
                    bodyBox.Left - boxesLeftRightSpacing,
                    bodyBox.Top - boxesTopBottomSpacing,
                    commonWidth, (2 * boxesTopBottomSpacing) + bodyBox.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ExtendedGraphics eg = new ExtendedGraphics(g);
            Rectangle rect = e.ClipRectangle;

            Pen pen = new Pen(frameColor, framePenWidth);

            //
            // Context
            //
            if (contextID != null)
            {
                int ctxSquareX = Width - framePenWidth - 2 - ctxSquareWidth;
                int ctxSquareY = framePenWidth + 2;

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

            //
            // Draw a round outer frame
            //
            eg.DrawRoundRectangle(pen, 0, 0, Width - framePenWidth, Height - framePenWidth, 4.0f);
        }

        [DllImport("gdi32.dll")]
        private static extern long BitBlt(
            IntPtr hdcDest,
            int xDest,
            int yDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSource,
            int xSrc,
            int ySrc,
            Int32 dwRop);

        const int SRCCOPY = 13369376;

        private Bitmap ControlToBitmap(Control ctrl)
        {
            Graphics grCtrl = Graphics.FromHwnd(ctrl.Handle);
            Bitmap result = new Bitmap(ctrl.Width, ctrl.Height, grCtrl);

            IntPtr hdcCtrl = grCtrl.GetHdc();
            Graphics grDest = Graphics.FromImage(result);
            IntPtr hdcDest = grDest.GetHdc();

            int sourceX, sourceY;
            if (ctrl is Form)
            {
                sourceX = ctrl.ClientSize.Width - ctrl.Width + 4;
                sourceY = ctrl.ClientSize.Height - ctrl.Height + 4;
            }
            else
            {
                sourceX = 0;
                sourceY = 0;
            }

            BitBlt(hdcDest, 0, 0, ctrl.Width, ctrl.Height, hdcCtrl, sourceX, sourceY, SRCCOPY);

            grCtrl.ReleaseHdc(hdcCtrl);
            grDest.ReleaseHdc(hdcDest);

            return result;
        }

        public void DrawToBitmapFull(Bitmap bitmap, int x, int y)
        {
            DrawToBitmap(bitmap, new Rectangle(x, y, bitmap.Width, bitmap.Height));

            Graphics g = Graphics.FromImage(bitmap);

            if (headlineBox.Text.Length > 0)
            {
                Bitmap headlineBitmap = ControlToBitmap(headlineBox);
                g.DrawImage(headlineBitmap, x + headlineBox.Left, y + headlineBox.Top);
            }

            if (bodyBox.Text.Length > 0)
            {
                Bitmap bodyBitmap = ControlToBitmap(bodyBox);
                g.DrawImage(bodyBitmap, x + bodyBox.Left, y + bodyBox.Top);
            }
        }

        public void SetBodyFromPreviewData(byte[] data, int maxSize)
        {
            int lineCount;

            BodyText = StaticUtils.ByteArrayToHexDump(data, maxSize, "", out lineCount);
        }

        public void SetBodyFromTruncatedPreviewData(byte[] data, int bytesRemaining)
        {
            int lineCount;

            BodyText = StaticUtils.ByteArrayToHexDump(data, -1, "", out lineCount, bytesRemaining);
        }

        public virtual void TransactionsCreated()
        {
        }

        public virtual void SessionsCreated()
        {
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 
            // VisualTransaction
            // 
            this.Name = "VisualTransaction";
            this.ResumeLayout(false);
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
