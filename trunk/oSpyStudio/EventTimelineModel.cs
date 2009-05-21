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
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;

public class EventTimelineModel : ITimelineModel
{
    private List<INode> m_nodes = new List<INode>();

    public IEnumerable<INode> Nodes
    {
        get
        {
            return m_nodes;
        }
    }

    public IEnumerable<INode> NodesReverse
    {
        get
        {
            for (int i = m_nodes.Count - 1; i >= 0; i--)
            {
                yield return m_nodes[i];
            }
        }
    }

    public EventTimelineModel(Dump dump)
    {
        foreach (KeyValuePair<uint, Event> pair in dump.Events)
        {
            m_nodes.Add(new EventNode(pair.Value));
        }
    }
}