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

namespace oSpyStudio.Widgets.Tests
{
    public class TestNode : ITimelineNode
    {
        private uint m_timestamp;
        private object m_context;
        private Point m_position;
        private Size m_allocation;

        public uint Timestamp
        {
            get
            {
                return m_timestamp;
            }
        }

        public object Context
        {
            get
            {
                return m_context;
            }
        }

        public Point Position
        {
            get
            {
                return m_position;
            }

            set
            {
                m_position = value;
            }
        }

        public Size Allocation
        {
            get
            {
                return m_allocation;
            }
        }

        public TestNode(uint timestamp, object context, Size allocation)
        {
            m_timestamp = timestamp;
            m_context = context;
            m_allocation = allocation;
        }
    }
}
