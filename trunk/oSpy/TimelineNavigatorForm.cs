using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using oSpy.Util;

namespace oSpy
{
    public partial class TimelineNavigatorForm : Form
    {
        protected Timeline timeline;
        public Timeline Timeline
        {
            get { return timeline; }
            set { timeline = value; }
        }

        public TimelineNavigatorForm(Timeline timeline)
        {
            InitializeComponent();

            this.timeline = timeline;

            timeline.LayoutChanged += new EventHandler(timeline_LayoutChanged);
            ResizeBegin += new EventHandler(TimelineNavigatorForm_ResizeBegin);
            ResizeEnd += new EventHandler(TimelineNavigatorForm_ResizeEnd);
        }

        private int prevWidth, prevHeight;

        private void TimelineNavigatorForm_ResizeBegin(object sender, EventArgs e)
        {
            prevWidth = ClientRectangle.Width;
            prevHeight = ClientRectangle.Height;
        }

        private void TimelineNavigatorForm_ResizeEnd(object sender, EventArgs e)
        {
            Rectangle fullRect = timeline.GetRealSize();
            if (fullRect.Width <= 0 || fullRect.Height <= 0)
                return;



            // FIXME: rewrite this logic when my brain is less fried
#if false
            int diffW = ClientRectangle.Width - prevWidth;
            int diffH = ClientRectangle.Height - prevHeight;

            float ratio;
            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;

            if (diffW > diffH)
            {
                ratio = (float)fullRect.Width / (float)fullRect.Height;
                w = (int)((float)ClientRectangle.Width * ratio);
            }
            else
            {
                ratio = (float)fullRect.Height / (float)fullRect.Width;
                h = (int)((float)ClientRectangle.Height * ratio);
            }

            SetClientSizeCore(w, h);
#endif

            UpdateNavBitmap();
        }

        private Bitmap fullBitmap, navBitmap;

        private void timeline_LayoutChanged(object sender, EventArgs e)
        {
            try
            {
                Rectangle fullRect = timeline.GetRealSize();
                if (fullRect.Width <= 0 || fullRect.Height <= 0)
                    return;

                try
                {
                    fullBitmap = new Bitmap(fullRect.Width, fullRect.Height);
                }
                catch (ArgumentException)
                {
                    fullBitmap = null;
                    return;
                }

                timeline.DrawToBitmap(fullBitmap, 0, 0);

                SetClientSizeCore(130, 130);

                float ratio = (float)fullRect.Width / (float)fullRect.Height;
                int w = (int)((float)ClientRectangle.Width * ratio);
                int h = ClientRectangle.Height;
                SetClientSizeCore(w, h);
            }
            finally
            {
                UpdateNavBitmap();
            }
        }

        private void UpdateNavBitmap()
        {
            if (fullBitmap == null || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            {
                navBitmap = null;
                return;
            }

            navBitmap = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            Graphics g = Graphics.FromImage(navBitmap);
            g.FillRectangle(new SolidBrush(timeline.BackColor), 0, 0, navBitmap.Width, navBitmap.Height);
            g.DrawImage(fullBitmap, 0, 0, navBitmap.Width, navBitmap.Height);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (navBitmap == null)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Red), e.ClipRectangle);
                return;
            }

            Graphics g = e.Graphics;
            g.DrawImage(navBitmap, 0, 0);


        }

        private void TimelineNavigatorForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            float relativeX = (float)e.X / (float)ClientRectangle.Width;
            float relativeY = (float)e.Y / (float)ClientRectangle.Height;

            int horzMin, horzMax;
            StaticUtils.GetScrollRange(timeline.Handle, StaticUtils.SB_HORZ, out horzMin, out horzMax);

            int horzNew = (int)((float)horzMax * relativeX);

            if (StaticUtils.SetScrollPos(timeline.Handle, StaticUtils.SB_HORZ, horzNew, true) != -1)
            {
                StaticUtils.SendMessage(timeline.Handle, StaticUtils.WM_HSCROLL, StaticUtils.SB_THUMBPOSITION + (0x10000 * horzNew), 0);
            }
            else
            {
                MessageBox.Show("SetScrollPos(SB_HORZ) failed");
            }

            int vertMin, vertMax;
            StaticUtils.GetScrollRange(timeline.Handle, StaticUtils.SB_VERT, out vertMin, out vertMax);

            int vertNew = (int)((float)vertMax * relativeY);

            if (StaticUtils.SetScrollPos(timeline.Handle, StaticUtils.SB_VERT, vertNew, true) != -1)
            {
                StaticUtils.SendMessage(timeline.Handle, StaticUtils.WM_VSCROLL, StaticUtils.SB_THUMBPOSITION + (0x10000 * vertNew), 0);
            }
            else
            {
                MessageBox.Show("SetScrollPos(SB_VERT) failed");
            }
        }
    }
}