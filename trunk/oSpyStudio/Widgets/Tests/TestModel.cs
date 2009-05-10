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
using System.Collections;
using System.Collections.Generic;

namespace oSpyStudio.Widgets.Tests
{
    public class TestModel : ITimelineModel
    {
        private List<ITimelineNode> m_nodes = new List<ITimelineNode>();

        public IEnumerable<ITimelineNode> Nodes
        {
            get
            {
                return m_nodes;
            }
        }

        public IEnumerable<ITimelineNode> NodesReverse
        {
            get
            {
                for (int i = m_nodes.Count - 1; i >= 0; i--)
                {
                    yield return m_nodes[i];
                }
            }
        }

        public TestNode CreateAddNode(uint timestamp, object context, Size allocation)
        {
            TestNode node = new TestNode(timestamp, context, allocation);
            m_nodes.Add(node);
            m_nodes.Sort(new TimelineNodeTimestampComparer());
            return node;
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