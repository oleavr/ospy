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
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace oSpyStudio.Widgets.Tests
{
    [TestFixture()]
    public class TimelineLayoutTest
    {
        [Test()]
        public void OneContextOneNode()
        {
            string context = "Context1";
            ITimelineNode node = new TestNode(10, context, new Size(100, 50));
            TimelineLayoutManager layout = new TimelineLayoutManager();
            layout.Add(node);
            Assert.That(layout.Nodes.Count, Is.EqualTo(1));
            Assert.That(layout.Nodes[0], Is.SameAs(node));
            Assert.That(node.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
        }

        [Test()]
        public void OneContextTwoNodes()
        {
            string context = "Context1";
            ITimelineNode nodeA = new TestNode(10, context, new Size(80, 50));
            ITimelineNode nodeB = new TestNode(20, context, new Size(100, 40));
            TimelineLayoutManager layout = new TimelineLayoutManager();
            layout.Add(nodeB);
            layout.Add(nodeA);
            Assert.That(layout.Nodes.Count, Is.EqualTo(2));
            Assert.That(layout.Nodes[0], Is.SameAs(nodeA));
            Assert.That(layout.Nodes[1], Is.SameAs(nodeB));
            Assert.That(nodeA.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
            Assert.That(nodeB.Position, Is.EqualTo(new Point(layout.XMargin + 80 + layout.XPadding, layout.YMargin)));
        }

        [Test()]
        public void TwoContextsOverlappingNodes()
        {
            string context1 = "Context1";
            ITimelineNode ctx1nodeA = new TestNode(10, context1, new Size(80, 50));
            ITimelineNode ctx1nodeB = new TestNode(20, context1, new Size(100, 40));
            string context2 = "Context2";
            ITimelineNode ctx2node = new TestNode(15, context2, new Size(50, 50));

            TimelineLayoutManager layout = new TimelineLayoutManager();
            layout.Add(ctx2node);
            layout.Add(ctx1nodeB);
            layout.Add(ctx1nodeA);

            Assert.That(layout.Nodes.Count, Is.EqualTo(3));
            Assert.That(layout.Nodes[0], Is.SameAs(ctx1nodeA));
            Assert.That(layout.Nodes[1], Is.SameAs(ctx2node));
            Assert.That(layout.Nodes[2], Is.SameAs(ctx1nodeB));

            Assert.That(ctx1nodeA.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
            Assert.That(ctx1nodeB.Position, Is.EqualTo(new Point(layout.XMargin + 80 + layout.XPadding, layout.YMargin)));

            Assert.That(ctx2node.Position.X, Is.EqualTo(ctx1nodeA.Position.X + (ctx1nodeA.Allocation.Width + layout.XPadding + ctx1nodeB.Allocation.Width) / 2));
            Assert.That(ctx2node.Position.Y, Is.EqualTo(layout.YMargin + 50 + layout.YMargin));
        }
    }
}
