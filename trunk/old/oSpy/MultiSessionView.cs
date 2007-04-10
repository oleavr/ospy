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
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Extended;
using System.IO;
using System.Runtime.InteropServices;
using oSpy.Util;
using System.Drawing.Drawing2D;

namespace oSpy
{
    public class Timeline : UserControl, IDrawToBitmapFull
    {
        public event EventHandler LayoutChanged;
        public event EventHandler ScrollPositionChanged;

        protected MultiSessionView msv;
        protected TimeRuler ruler;

        protected int realWidth;
        protected int realHeight;

        public int ColumnsStartPos
        {
            get { return ruler.Width; }
        }

        public Timeline(MultiSessionView msw)
        {
            this.msv = msw;

            ruler = new TimeRuler();
            ruler.Parent = this;
            ruler.Left = 0;
            ruler.Top = 0;

            this.AutoScroll = true;
            this.BackColor = Color.White;
        }

        public void UpdateLayout()
        {
            int x = ColumnsStartPos;

            ruler.Clear();

            // First off, create a flat list of all transactions in all sessions,
            // and make a lookup table for each transaction's column index
            List<VisualTransaction> transactions = new List<VisualTransaction>();
            Dictionary<VisualTransaction, int> transactionToColIndex = new Dictionary<VisualTransaction, int>();

            int colIndex = 0;
            foreach (VisualSession session in msv.Sessions)
            {
                foreach (VisualTransaction transaction in session.Transactions)
                {
                    transactions.Add(transaction);
                    transactionToColIndex.Add(transaction, colIndex);
                }

                colIndex++;
            }

            // Sort the transactions by StartTime
            transactions.Sort();

            int y = 0;

            // Now it's time to rock 'n roll!
            while (transactions.Count > 0)
            {
                // Find all transactions with the same time and the height of the highest one of them
                List<VisualTransaction> sameTime = new List<VisualTransaction>(1);
                DateTime curTime = transactions[0].StartTime;
                int highest = 0;

                for (int i = 0; i < transactions.Count; i++)
                {
                    VisualTransaction curTransaction = transactions[i];

                    if (sameTime.Count > 0)
                    {
                        if (curTransaction.StartTime != curTime)
                            break;
                    }

                    sameTime.Add(curTransaction);
                    if (curTransaction.Height > highest)
                        highest = curTransaction.Height;
                }

                // Remove them from the list
                transactions.RemoveRange(0, sameTime.Count);

                Dictionary<int, int> columnOffsets = new Dictionary<int, int>();

                // Lay them out
                foreach (VisualTransaction transaction in sameTime)
                {
                    colIndex = transactionToColIndex[transaction];

                    transaction.Parent = this;
                    transaction.Left = ColumnsStartPos + (colIndex * (msv.ColumnWidth + msv.ColumnSpacing));

                    int colY = 0;

                    if (columnOffsets.ContainsKey(colIndex))
                    {
                        colY = columnOffsets[colIndex] + 10;
                    }

                    transaction.Top = y + colY;

                    columnOffsets[colIndex] = colY + transaction.Height;
                }

                foreach (int offset in columnOffsets.Values)
                {
                    if (offset > highest)
                        highest = offset;
                }

                y += highest + 10;

                ruler.AddMark(sameTime[0].StartTime, highest + 10);
            }

            realWidth = ruler.Width + (msv.Sessions.Length * (msv.ColumnWidth + msv.ColumnSpacing));
            realHeight = y;

            if (LayoutChanged != null)
                LayoutChanged(this, new EventArgs());
        }

        public Rectangle GetRealSize()
        {
            return new Rectangle(0, 0, realWidth, realHeight);
        }

        public void DrawToBitmapFull(Bitmap bitmap, int x, int y)
        {
            foreach (Control control in Controls)
            {
                IDrawToBitmapFull dtbfControl = control as IDrawToBitmapFull;

                dtbfControl.DrawToBitmapFull(bitmap, x + control.Left, y + control.Top);
            }
        }

        public void DrawToBitmapScaled(Bitmap bitmap)
        {
            float ratioX = (float)bitmap.Width / (float)realWidth;
            float ratioY = (float)bitmap.Height / (float)realHeight;

            Matrix matrix = new Matrix();
            matrix.Scale(ratioX, ratioY);

            Graphics g = Graphics.FromImage(bitmap);
            g.Transform = matrix;

            DrawToGraphics(g);
        }

        public void DrawToGraphics(Graphics g)
        {
            foreach (Control control in Controls)
            {
                IDrawToBitmapFull dtbfControl = control as IDrawToBitmapFull;

                Bitmap bitmap = new Bitmap(control.Width, control.Height);

                dtbfControl.DrawToBitmapFull(bitmap, 0, 0);

                g.DrawImage(bitmap, control.Left, control.Top);
            }
        }

        public void SetScrollPosition(int x, int y)
        {
            AutoScrollPosition = new Point(x, y);

            if (ScrollPositionChanged != null)
                ScrollPositionChanged(this, new EventArgs());
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);

            if (ScrollPositionChanged != null)
                ScrollPositionChanged(this, new EventArgs());
        }
    }

    public class TimeRuler : UserControl, IDrawToBitmapFull
    {
        protected int requiredHeight;
        protected List<KeyValuePair<int, DateTime>> marks;

        public TimeRuler()
        {
            marks = new List<KeyValuePair<int, DateTime>>();

            BackColor = Color.White;
            ForeColor = Color.Black;
            Font = new Font("Lucida Console", 8, FontStyle.Regular);

            Clear();
        }

        public void Clear()
        {
            requiredHeight = 0;
            marks.Clear();
            CalculateSize();
        }

        public void AddMark(DateTime time, int height)
        {
            marks.Add(new KeyValuePair<int, DateTime>(height, time));
            requiredHeight += height;
            CalculateSize();
        }

        private float strWidth = 0;
        private int spacing = 5;

        private void CalculateSize()
        {
            Graphics g = this.CreateGraphics();
            strWidth = g.MeasureString(FormatTime(DateTime.Now), Font).Width;

            Width = (int)strWidth + spacing;
            Height = requiredHeight;

            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            CalculateSize();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            CalculateSize();
        }

        private string FormatTime(DateTime t)
        {
            return String.Format("[{0}]", t.ToLongTimeString());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = e.ClipRectangle;

            Brush bgBrush = new SolidBrush(BackColor);
            Brush fgBrush = new SolidBrush(ForeColor);

            g.FillRectangle(bgBrush, r);

            int y = 3;
            string lastTimeStr = null;
            foreach (KeyValuePair<int, DateTime> mark in marks)
            {
                string timeStr = FormatTime(mark.Value);
                if (timeStr != lastTimeStr)
                {
                    g.DrawString(timeStr, Font, fgBrush, (Width / 2.0f) - (strWidth / 2.0f), y);
                    lastTimeStr = timeStr;
                }

                y += mark.Key;
            }
        }

        public void DrawToBitmapFull(Bitmap bitmap, int x, int y)
        {
            DrawToBitmap(bitmap, new Rectangle(x, y, bitmap.Width, bitmap.Height));
        }
    }

    public delegate void TransactionDoubleClickHandler(VisualTransaction transaction);
    public delegate void SessionsChangedHandler(VisualSession[] newSessions);

    public class MultiSessionView : UserControl
    {
        protected EventHandler transactionSizeChangedHandler, transactionDoubleClickHandler;

        protected List<VisualSession> sessions;
        public VisualSession[] Sessions
        {
            get { return sessions.ToArray(); }
            set { DeleteSessions(sessions.ToArray(), false); AddSessions(value); }
        }

        protected int colWidth;
        public int ColumnWidth
        {
            get { return colWidth; }
        }

        protected int colSpacing;
        public int ColumnSpacing
        {
            get { return colSpacing; }
        }

        public event SessionsChangedHandler SessionsChanged;
        public event TransactionDoubleClickHandler TransactionDoubleClick;

        protected ColorPool colorPool;
        protected Timeline timeline;
        protected TimelineNavigatorForm navigatorForm;

        public MultiSessionView()
        {
            transactionSizeChangedHandler = new EventHandler(transaction_SizeChanged);
            transactionDoubleClickHandler = new EventHandler(transaction_DoubleClick);

            sessions = new List<VisualSession>();

            this.BackColor = Color.White;
            this.DoubleBuffered = true;

            timeline = new Timeline(this);
            timeline.Parent = this;
            timeline.SizeChanged += new EventHandler(timeline_SizeChanged);
            timeline.ScrollPositionChanged += new EventHandler(timeline_Scroll);
            timeline.Click += new EventHandler(timeline_Click);

            navigatorForm = new TimelineNavigatorForm(timeline);

            Clear();
        }


        //
        // API
        //

        public void Clear()
        {
            colorPool = new ColorPool();

            // The first color is white, skip it
            colorPool.GetColorForId("MultiSessionView1");

            DeleteSessions(sessions.ToArray());
        }

        public void AddSessions(VisualSession[] sessions)
        {
            AddSessions(sessions, true);
        }

        public void AddSessions(VisualSession[] sessions, bool update)
        {
            this.sessions.AddRange(sessions);

            EmitSessionsChanged();

            foreach (VisualSession session in sessions)
            {
                foreach (VisualTransaction transaction in session.Transactions)
                {
                    transaction.Parent = null;
                    transaction.Left = 0;
                    transaction.Top = 0;
                    transaction.SizeChanged += transactionSizeChangedHandler;
                    transaction.DoubleClick += transactionDoubleClickHandler;
                }
            }

            if (update)
            {
                UpdateView();
                EmitSessionsChanged();
            }
        }

        public void DeleteSessions(VisualSession[] sessions)
        {
            DeleteSessions(sessions, true);
        }

        public void DeleteSessions(VisualSession[] sessions, bool update)
        {
            foreach (VisualSession session in sessions)
            {
                foreach (VisualTransaction transaction in session.Transactions)
                {
                    transaction.Parent = null;
                    //transaction.Left = 0;
                    //transaction.Top = 0;
                    //transaction.SizeChanged -= transactionSizeChangedHandler;
                    //transaction.DoubleClick -= transactionDoubleClickHandler;
                }

                this.sessions.Remove(session);
            }

            if (update)
            {
                UpdateView();
                EmitSessionsChanged();
            }
        }

        public void SaveToPng(string filename)
        {
            Rectangle tlRect = timeline.GetRealSize();

            int width = headingBitmap.Width;
            int height = timelineRect.Y + tlRect.Height;

            Bitmap bitmap = null;

            for (bitmap = null; bitmap == null; )
            {
                try
                {
                    bitmap = new Bitmap(width, height);
                }
                catch (ArgumentException)
                {
                    if (width > height)
                    {
                        width /= 2;
                    }
                    else
                    {
                        height /= 2;
                    }
                }
            }

            Graphics g = Graphics.FromImage(bitmap);
            g.DrawImage(headingBitmap, new Rectangle(0, 0, width, headingBitmap.Height));

            timeline.DrawToBitmapFull(bitmap, 0, timelineRect.Y);

            Stream stream = File.Open(filename, FileMode.Create);
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Close();
        }

        public void AutoPositionNavigatorForm()
        {
            Point p = new Point(timeline.ClientRectangle.Width - navigatorForm.Width - 5,
                                timeline.ClientRectangle.Height - navigatorForm.Height - 5);
            navigatorForm.Location = timeline.PointToScreen(p);
        }


        //
        // Helper functions
        //

        private void EmitSessionsChanged()
        {
            if (SessionsChanged != null)
                SessionsChanged(sessions.ToArray());
        }

        private Rectangle contentRect;
        private Rectangle headingRect, headingColsRect;
        private Rectangle timelineRect;

        private Bitmap headingBitmap;

        private void Recalibrate()
        {
            contentRect.X = this.ClientRectangle.X;
            contentRect.Y = this.ClientRectangle.Y;
            contentRect.Width = this.ClientRectangle.Width - contentRect.X;
            contentRect.Height = this.ClientRectangle.Height - contentRect.Y;

            headingRect.X = contentRect.X;
            headingRect.Y = contentRect.Y + 5;
            headingRect.Width = contentRect.Width;
            headingRect.Height = 20;

            headingColsRect.X = headingRect.X + timeline.ColumnsStartPos;
            headingColsRect.Y = headingRect.Y;
            headingColsRect.Width = headingRect.Width - headingColsRect.X;
            headingColsRect.Height = headingRect.Height;

            timelineRect.X = contentRect.X;
            timelineRect.Y = headingRect.Y + headingRect.Height + 5;
            timelineRect.Width = contentRect.Width;
            timelineRect.Height = contentRect.Height - timelineRect.Y;

            if (sessions.Count > 0)
                colWidth = sessions[0].Transactions[0].Width;
            else
                colWidth = 100;

            colSpacing = 10;

            timeline.Left = timelineRect.X;
            timeline.Top = timelineRect.Y;
            timeline.Width = timelineRect.Width;
            timeline.Height = timelineRect.Height;

            headingBitmap = GetHeadingBitmap();
        }

        private bool updatingView = false;

        private void UpdateView()
        {
            if (updatingView)
                return;

            updatingView = true;

            Recalibrate();

            Invalidate();

            selectedSessions.Clear();

            timeline.UpdateLayout();

            updatingView = false;
        }

        private Bitmap GetHeadingBitmap()
        {
            int w = headingColsRect.X + (sessions.Count * (colWidth + colSpacing));
            int h = headingColsRect.Y + headingColsRect.Height + 3; // FIXME

            Bitmap bitmap = new Bitmap(w, h);

            Graphics g = Graphics.FromImage(bitmap);
            ExtendedGraphics eg = new ExtendedGraphics(g);

            Brush fgBrush = new SolidBrush(Color.White);
            Font headerFont = new Font("Tahoma", 10, FontStyle.Bold);

            int x = headingColsRect.X;

            Pen selectedPen = new Pen(Color.Black, 3);

            foreach (VisualSession stream in sessions)
            {
                string localEpStr = stream.LocalEndpoint.ToString();
                string remoteEpStr = stream.RemoteEndpoint.ToString();

                string str;
                if (localEpStr.Length > 0 && remoteEpStr.Length > 0)
                    str = String.Format("{0} <-> {1}", localEpStr, remoteEpStr);
                else
                    str = "<UNKNOWN ENDPOINTS>";

                Brush bgBrush = new SolidBrush(colorPool.GetColorForId(Convert.ToString(str.GetHashCode())));

                eg.FillRoundRectangle(bgBrush, x, headingColsRect.Y, colWidth, headingColsRect.Height, 2.0f);

                if (selectedSessions.ContainsKey(stream))
                {
                    eg.DrawRoundRectangle(selectedPen, x, headingColsRect.Y, colWidth, headingColsRect.Height, 2.0f);
                }

                SizeF fs = g.MeasureString(str, headerFont);

                g.DrawString(str, headerFont, fgBrush,
                    x + (colWidth / 2) - (fs.Width / 2),
                    headingColsRect.Y + ((headingColsRect.Height / 2) - (fs.Height / 2)));

                x += colWidth + colSpacing;
            }

            return bitmap;
        }

        private VisualSession GetVisualSessionAtCoordinates(int x, int y)
        {
            if (x < headingColsRect.X || x > headingColsRect.X + headingColsRect.Width ||
                y < headingColsRect.Y || y > headingColsRect.Y + headingColsRect.Height)
            {
                return null;
            }

            int virtX = scrollOffset + x;
            int index = virtX / (colWidth + colSpacing);
            if (index < sessions.Count)
            {
                return sessions[index];
            }

            return null;
        }


        //
        // Event handlers
        //

        protected override void OnPaint(PaintEventArgs e)
        {
            Bitmap bitmap = headingBitmap;

            e.Graphics.DrawImage(bitmap,
                new Rectangle(0, 0, bitmap.Width - scrollOffset, bitmap.Height),
                new Rectangle(scrollOffset, 0, bitmap.Width - scrollOffset, bitmap.Height),
                GraphicsUnit.Pixel);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (DesignMode)
                return;

            if (Visible)
            {
                AutoPositionNavigatorForm();
                navigatorForm.Show();
            }
            else
            {
                navigatorForm.Hide();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            navigatorForm.Close();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            Recalibrate();
        }

        private Dictionary<VisualSession, bool> selectedSessions = new Dictionary<VisualSession, bool>();

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            VisualSession session = GetVisualSessionAtCoordinates(e.X, e.Y);
            if (session == null)
                return;

            if (!selectedSessions.ContainsKey(session))
                selectedSessions[session] = true;
            else
                selectedSessions.Remove(session);

            headingBitmap = GetHeadingBitmap();
            Invalidate();
        }

        private VisualSession ctxMenuCurSession;

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Right)
                return;

            ctxMenuCurSession = GetVisualSessionAtCoordinates(e.X, e.Y);
            if (ctxMenuCurSession == null)
                return;

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripItem item = new ToolStripMenuItem("Delete selected", null, new EventHandler(headingCol_OnDeleteClick));
            menu.Items.Add(item);
            item.Enabled = (selectedSessions.Count > 0);
            menu.Show(this, e.X, e.Y);
        }

        private void headingCol_OnDeleteClick(object sender, EventArgs e)
        {
            VisualSession[] sessions = new VisualSession[selectedSessions.Keys.Count];
            selectedSessions.Keys.CopyTo(sessions, 0);
            selectedSessions.Clear();
            DeleteSessions(sessions);
        }

        private void timeline_Click(object sender, EventArgs e)
        {
            OnClick(e);
        }

        private void transaction_SizeChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void transaction_DoubleClick(object sender, EventArgs e)
        {
            if (transactionDoubleClickHandler != null)
                TransactionDoubleClick(sender as VisualTransaction);
        }

        private int scrollOffset = 0;

        private void timeline_Scroll(object sender, EventArgs e)
        {
            scrollOffset = -timeline.AutoScrollPosition.X;
            Invalidate();
        }

        private void timeline_SizeChanged(object sender, EventArgs e)
        {
            scrollOffset = timeline.AutoScrollPosition.X;
            Invalidate();

            AutoPositionNavigatorForm();
        }
    }
}
