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
using Gtk;

namespace oSpyStudio.Widgets
{
    public class TimelineView : Fixed
    {
        private ITimelineModel m_model;
        private INodeRenderer m_renderer;

        public TimelineView(ITimelineModel model, INodeRenderer renderer)
        {
            m_model = model;
            m_renderer = renderer;

            foreach (INode node in m_model.Nodes)
            {
                Widget widget = m_renderer.Render(node);
                if (widget == null)
                    throw new NotImplementedException("Renderer returned null");
                widget.Show();
                Console.WriteLine("{0}, {1}", node.Position.X, node.Position.Y);
                Put(widget, (int) node.Position.X, (int) node.Position.Y);
            }
        }
    }
}