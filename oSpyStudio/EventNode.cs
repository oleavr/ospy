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
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;

public class EventNode : INode
{
    private Event m_event;
    private object m_context;
    private Point m_position = new Point(0, 0);
    private Size m_allocation = new Size(320, 240);

    public Event Event
    {
        get
        {
            return m_event;
        }
    }

    public uint Timestamp
    {
        get
        {
            return (uint) m_event.Timestamp.ToFileTimeUtc();
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

    public EventNode(Event ev)
    {
        m_event = ev;

        foreach (ITag tag in m_event.Tags)
        {
            if (tag is ResourceTag)
                m_context = tag;
        }

        if (m_context == null)
            m_context = this;
    }
}