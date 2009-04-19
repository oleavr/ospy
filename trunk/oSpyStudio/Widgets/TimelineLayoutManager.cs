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
        protected uint m_rowCount;

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

        public uint RowCount
        {
            get
            {
                return m_rowCount;
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

            NodeList remainingNodes = new NodeList(m_nodes);
            RangeList allocatedRanges = new RangeList();
            RowList rows = new RowList();

            while (remainingNodes.Count > 0)
            {
                NodeList relatedNodes = remainingNodes.PopFirstRangeOfRelatedNodes();
                uint startTime = relatedNodes[0].Timestamp;
                uint endTime = relatedNodes[relatedNodes.Count - 1].Timestamp; // FIXME: need Duration
                Range range = new Range(startTime, endTime, relatedNodes);

                int rowIndex = allocatedRanges.GetNumberOfIntersectingRanges(range);
                allocatedRanges.Add(range);

                if (rowIndex >= rows.Count)
                    rows.Add(new Row());
                Row row = rows[rowIndex];
                row.AddRange(range);
            }

            uint x = m_xmargin;
            uint y = m_ymargin;
            foreach (Row row in rows)
            {
                
            }

            m_rowCount = (uint) rows.Count;
        }

        internal class NodeList : List<ITimelineNode>
        {
            public NodeList()
                : base()
            {
            }

            public NodeList(IEnumerable<ITimelineNode> collection)
                : base(collection)
            {
            }

            public NodeList PopFirstRangeOfRelatedNodes()
            {
                NodeList relatedNodes = new NodeList();

                ITimelineNode firstNode = this[0];
                relatedNodes.Add(firstNode);
                RemoveAt(0);

                foreach (ITimelineNode node in this)
                {
                    if (node.Context == firstNode.Context)
                        relatedNodes.Add(node);
                }

                for (int i = 1; i < relatedNodes.Count; i++)
                    Remove(relatedNodes[i]);

                return relatedNodes;
            }
        }

        internal class Range
        {
            public readonly uint Start;
            public readonly uint End;
            public readonly NodeList Nodes;

            public Range(uint start, uint end, NodeList nodes)
            {
                Start = start;
                End = end;
                Nodes = nodes;
            }
        }

        internal class RangeList : List<Range>
        {
            public int GetNumberOfIntersectingRanges(Range range)
            {
                int numIntersections = 0;

                foreach (Range curRange in this)
                {
                    if ((curRange.Start >= range.Start && curRange.Start <= range.End) ||
                        (curRange.End >= range.Start && curRange.End <= range.End))
                    {
                        numIntersections++;
                    }
                }

                return numIntersections;
            }
        }

        internal class Row
        {
            public RangeList Ranges = new RangeList();

            public void AddRange(Range range)
            {
                Ranges.Add(range);
            }
        }

        internal class RowList : List<Row>
        {
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