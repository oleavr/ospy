//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Threading;
using System.Drawing;
using System.ComponentModel;

namespace oSpy
{
    public class SearchBox : TextBox
    {
        private MatchView matchView;
        private PictureBox searchPic;
        private bool isInactive = true;
        private string idleText;
        private ICollection<ListViewItem> suggestions;

        [Category("Appearance")]
        public string IdleText
        {
            get
            {
                return idleText;
            }

            set
            {
                idleText = value;
                UpdateUi();
            }
        }

        public ICollection<ListViewItem> Suggestions
        {
            get
            {
                return suggestions;
            }
        }

        public delegate void SuggestionActivatedHandler(object sender, SuggestionActivatedEventArgs e);

        [Category("Action")]
        public event SuggestionActivatedHandler SuggestionActivated;

        public SearchBox()
        {
            matchView = new MatchView(this);
            matchView.Visible = false;
            matchView.View = View.List;
            matchView.MultiSelect = false;

            searchPic = new PictureBox();
            searchPic.Image = oSpy.Properties.Resources.SearchImg;
            searchPic.Size = searchPic.PreferredSize;
            searchPic.Parent = this;

            UpdateUi();
        }

        private void UpdateUi()
        {
            if (suggestions != null)
            {
                if (isInactive)
                {
                    Text = idleText;
                    Font = new Font(Font, FontStyle.Italic);
                    ForeColor = Color.Gray;
                }
                else
                {
                    Text = "";
                    Font = new Font(Font, FontStyle.Regular);
                    ForeColor = Color.Black;
                }

                Enabled = true;
            }
            else
            {
                Text = "Please wait...";
                Enabled = false;
            }
        }

        private void BeginInput()
        {
            Text = "";
            isInactive = false;
            UpdateUi();
        }

        private void CancelInput()
        {
            isInactive = true;
            Text = "";
            matchView.Hide();
            matchView.Clear();
            UpdateUi();
        }

        private delegate void UpdateSuggestionsHandler(ICollection<ListViewItem> items, ImageList imageLst);

        public void UpdateSuggestions(ICollection<ListViewItem> items, ImageList imageLst)
        {
            if (InvokeRequired)
            {
                Invoke(new UpdateSuggestionsHandler(UpdateSuggestions), items, imageLst);
                return;
            }

            suggestions = items;
            matchView.SmallImageList = imageLst;
            matchView.LargeImageList = imageLst;

            UpdateUi();

            if (suggestions != null)
                Focus();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            searchPic.Left = Width - searchPic.Width - 5;
            searchPic.Top = 0;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            if (isInactive)
                BeginInput();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            if (!matchView.Visible)
                CancelInput();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (isInactive)
            {
                return;
            }
            else if (suggestions == null || Text.Trim().Length == 0)
            {
                matchView.Hide();
                return;
            }

            matchView.Clear();

            string inputLower = Text.ToLower();

            foreach (ListViewItem item in suggestions)
            {
                string textLower = item.Text.ToLower();
                if (textLower.IndexOf(inputLower) >= 0)
                    matchView.Items.Add(item);
            }

            PositionMatchView();

            matchView.SelectedIndices.Clear();
            if (matchView.Items.Count > 0)
                matchView.SelectedIndices.Add(0);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = false;

            if (matchView.Visible)
            {
                if (e.KeyCode == Keys.Down || e.KeyValue == 13)
                {
                    matchView.Focus();
                    e.Handled = true;
                }
            }

            if (!e.Handled)
                base.OnKeyDown(e);
        }

        private void PositionMatchView()
        {
            Control topmostParent = Parent;
            while (topmostParent.Parent != null)
            {
                topmostParent = topmostParent.Parent;
            }

            Size mvSize = matchView.PreferredSize;
            mvSize.Width = Width;
            mvSize.Height = Math.Min(mvSize.Height, 100);

            Rectangle tbScreen = this.RectangleToScreen(new Rectangle(0, 0, Width, Height));
            Rectangle mvScreen = new Rectangle(tbScreen.X, tbScreen.Bottom + 1, mvSize.Width, mvSize.Height);
            Rectangle mvClient = topmostParent.RectangleToClient(mvScreen);
            matchView.Parent = topmostParent;
            matchView.SetBounds(mvClient.X, mvClient.Y, mvClient.Width, mvClient.Height);
            matchView.BringToFront();
            matchView.Show();
        }

        private void SelectMatch(ListViewItem item)
        {
            if (SuggestionActivated != null)
                SuggestionActivated(this, new SuggestionActivatedEventArgs(item));
            CancelInput();
        }
        
        private class MatchView : ListView
        {
            private SearchBox textBox;

            public MatchView(SearchBox textBox)
            {
                this.textBox = textBox;
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Back)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
                else if (e.KeyValue == 13)
                {
                    if (TrySelectMatch())
                        e.Handled = true;
                }

                if (!e.Handled)
                    base.OnKeyDown(e);
            }

            protected override void OnItemActivate(EventArgs e)
            {
                base.OnItemActivate(e);
                TrySelectMatch();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                textBox.CancelInput();
            }

            private bool TrySelectMatch()
            {
                var selected = SelectedItems;
                if (selected.Count == 0)
                    return false;
                textBox.SelectMatch(selected[0]);
                return true;
            }
        }
    }


    public class SuggestionActivatedEventArgs : EventArgs
    {
        private ListViewItem item;

        public object Item
        {
            get
            {
                return item;
            }
        }

        internal SuggestionActivatedEventArgs(ListViewItem item)
        {
            this.item = item;
        }
    }
}
