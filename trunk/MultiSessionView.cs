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
using System.IO;
using System.Runtime.InteropServices;

namespace oSpy
{
    public delegate void SessionsChangedHandler(VisualSession[] newSessions);

    public class MultiSessionView : UserControl
    {
        protected class Timeline : UserControl
        {
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

                // First off, create a flat list of all transactions in all streams,
                // and make a lookup table for each transaction's column index
                List<VisualTransaction> transactions = new List<VisualTransaction>();
                Dictionary<VisualTransaction, int> transactionToColIndex = new Dictionary<VisualTransaction, int>();

                int colIndex = 0;
                foreach (VisualSession stream in msv.Streams)
                {
                    foreach (VisualTransaction transaction in stream.Transactions)
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
                        transaction.Left = ColumnsStartPos + (colIndex * (msv.colWidth + msv.colSpacing));

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

                realWidth = ruler.Width + (msv.streams.Length * (msv.colWidth + msv.colSpacing));
                realHeight = y;
            }

            public Rectangle GetRealSize()
            {
                return new Rectangle(0, 0, realWidth, realHeight);
            }

            public void DrawToBitmap(Bitmap bitmap, int x, int y)
            {
                foreach (Control control in Controls)
                {
                    Rectangle rect = new Rectangle(x + control.Left,
                        y + control.Top, realWidth, realHeight);
                    control.DrawToBitmap(bitmap, rect);
                }
            }
        }

        protected class TimeRuler : UserControl
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

                Width = (int) strWidth + spacing;
                Height = requiredHeight;
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
                //return String.Format("[{0:00}:{1:00}:{2:00}]",
                //    t.Hour, t.Minute, t.Second);
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
        }

        protected VisualSession[] streams;
        public VisualSession[] Streams
        {
            get { return streams; }
            set
            {
                streams = value;
                UpdateView();
                if (SessionsChanged != null)
                    SessionsChanged(value);
            }
        }

        public event SessionsChangedHandler SessionsChanged;

        protected ColorPool colorPool;
        protected Timeline timeline;

        public MultiSessionView()
        {
            streams = new VisualSession[0];

            this.BackColor = Color.White;

            colorPool = new ColorPool();

            // The first color is white, skip it
            colorPool.GetColorForId("MultiStreamView1");

            timeline = new Timeline(this);
            timeline.Parent = this;
            timeline.SizeChanged += new EventHandler(timeline_SizeChanged);
            timeline.Scroll += new ScrollEventHandler(timeline_Scroll);
            timeline.Click += new EventHandler(timeline_Click);

            Recalibrate();
        }

        private void timeline_Click(object sender, EventArgs e)
        {
            OnClick(e);
        }

        private int contentX, contentY, contentWidth, contentHeight;

        private int headingX, headingY, headingHeight;
        private int timelineX, timelineY, timelineWidth, timelineHeight;

        private int colWidth;
        private int colSpacing;

        private void Recalibrate()
        {
            contentX = this.ClientRectangle.X;
            contentY = this.ClientRectangle.Y;
            contentWidth = this.ClientRectangle.Width - contentX;
            contentHeight = this.ClientRectangle.Height - contentY;

            headingX = contentX;
            headingY = contentY + 5;
            headingHeight = 20;

            timelineX = contentX;
            timelineY = headingY + headingHeight + 5;
            timelineWidth = contentWidth;
            timelineHeight = contentHeight - timelineY;

            if (streams.Length > 0)
                colWidth = streams[0].Transactions[0].Width;
            else
                colWidth = 100;

            colSpacing = 10;

            timeline.Left = timelineX;
            timeline.Top = timelineY;
            timeline.Width = timelineWidth;
            timeline.Height = timelineHeight;
        }

        private void UpdateView()
        {
            if (streams.Length == 0)
                return;

            foreach (VisualSession stream in streams)
            {
                foreach (VisualTransaction transaction in stream.Transactions)
                {
                    transaction.SizeChanged += new EventHandler(transaction_SizeChanged);
                }
            }

            Recalibrate();

            Invalidate();

            timeline.UpdateLayout();
        }

        private void transaction_SizeChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            Recalibrate();
        }

        private int scrollOffset = 0;

        void timeline_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
            {
                scrollOffset = e.NewValue;
                Invalidate();
            }
        }

        private void timeline_SizeChanged(object sender, EventArgs e)
        {
            scrollOffset = Util.GetScrollPos(timeline.Handle, Util.SB_HORZ);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Bitmap bitmap = GetHeadersBitmap();

            e.Graphics.DrawImage(bitmap,
                new Rectangle(0, 0, bitmap.Width - scrollOffset, bitmap.Height),
                new Rectangle(scrollOffset, 0, bitmap.Width - scrollOffset, bitmap.Height),
                GraphicsUnit.Pixel);
        }

        private Bitmap GetHeadersBitmap()
        {
            int w = headingX + timeline.ColumnsStartPos + (streams.Length * (colWidth + colSpacing));
            int h = headingY + headingHeight;

            Bitmap bitmap = new Bitmap(w, h);

            Graphics g = Graphics.FromImage(bitmap);
            ExtendedGraphics eg = new ExtendedGraphics(g);

            Brush fgBrush = new SolidBrush(Color.White);
            Font headerFont = new Font("Tahoma", 10, FontStyle.Bold);

            int x = headingX + timeline.ColumnsStartPos;

            foreach (VisualSession stream in streams)
            {
                string str = String.Format("{0} <-> {1}", stream.LocalEndpoint, stream.RemoteEndpoint);

                Brush bgBrush = new SolidBrush(colorPool.GetColorForId(Convert.ToString(str.GetHashCode())));

                eg.FillRoundRectangle(bgBrush, x, headingY, colWidth, headingHeight, 2.0f);

                SizeF fs = g.MeasureString(str, headerFont);

                g.DrawString(str, headerFont, fgBrush,
                    x + (colWidth / 2) - (fs.Width / 2),
                    headingY + ((headingHeight / 2) - (fs.Height / 2)));

                x += colWidth + colSpacing;
            }

            return bitmap;
        }

        public void SaveToPng(string filename)
        {
            Bitmap headersBitmap = GetHeadersBitmap();

            Rectangle tlRect = timeline.GetRealSize();

            Bitmap bitmap = new Bitmap(headersBitmap.Width, timelineY + tlRect.Height);

            Graphics g = Graphics.FromImage(bitmap);
            g.DrawImage(headersBitmap, new Rectangle(0, 0, headersBitmap.Width, headersBitmap.Height));

            timeline.DrawToBitmap(bitmap, 0, timelineY);

            Stream stream = File.Open(filename, FileMode.Create);
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Close();
        }
    }
}
