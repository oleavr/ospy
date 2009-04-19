//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This library is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;

namespace oSpyStudio.Widgets
{
    public class TimelineLayoutManager
    {
        protected readonly uint m_xmargin = 5;
        protected readonly uint m_ymargin = 10;
        protected readonly uint m_xpadding = 3;
        protected readonly uint m_ypadding = 6;
        protected List<ITimelineNode> m_nodes = new List<ITimelineNode>();

        public uint XMargin
        {
            get
            {
                return m_xmargin;
            }
        }

        public uint YMargin
        {
            get
            {
                return m_ymargin;
            }
        }

        public uint XPadding
        {
            get
            {
                return m_xpadding;
            }
        }

        public uint YPadding
        {
            get
            {
                return m_ypadding;
            }
        }

        public List<ITimelineNode> Nodes
        {
            get
            {
                return m_nodes;
            }
        }
        
        public void Add(ITimelineNode node)
        {
            m_nodes.Add(node);
            DoLayout();
        }

        private void DoLayout()
        {
            m_nodes.Sort(new TimelineNodeTimestampComparer());

            uint x = m_xmargin;
            uint y = m_ymargin;
            foreach (ITimelineNode node in m_nodes)
            {
                node.Position = new Point(x, y);

                x += node.Allocation.Width + m_xpadding;
            }
        }
    }

    internal class TimelineNodeTimestampComparer : IComparer<ITimelineNode>
    {
        public int Compare(ITimelineNode a, ITimelineNode b)
        {
            if (a.Timestamp > b.Timestamp)
                return 1;
            else if (a.Timestamp < b.Timestamp)
                return -1;
            else
                return 0;
        }
    }
}