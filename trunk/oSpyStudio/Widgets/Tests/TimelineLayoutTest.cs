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
            TestModel model = new TestModel();
            INode node = model.CreateAddNode(10, context, new Size(100, 50));
            TimelineLayoutManager layout = new TimelineLayoutManager(model);
            layout.Update();
            Assert.That(node.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
        }

        [Test()]
        public void OneContextTwoNodes()
        {
            string context = "Context1";
            TestModel model = new TestModel();
            INode nodeA = model.CreateAddNode(10, context, new Size(80, 50));
            INode nodeB = model.CreateAddNode(20, context, new Size(100, 40));
            TimelineLayoutManager layout = new TimelineLayoutManager(model);
            layout.Update();
            Assert.That(nodeA.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
            Assert.That(nodeB.Position, Is.EqualTo(new Point(layout.XMargin + nodeA.Allocation.Width + layout.XPadding, layout.YMargin)));
        }

        [Test()]
        public void TwoContextsInSequence()
        {
            TestModel model = new TestModel();
            string context1 = "Context1";
            INode ctx1nodeA = model.CreateAddNode(10, context1, new Size(80, 50));
            INode ctx1nodeB = model.CreateAddNode(20, context1, new Size(100, 40));
            string context2 = "Context2";
            INode ctx2node = model.CreateAddNode(30, context2, new Size(50, 30));

            TimelineLayoutManager layout = new TimelineLayoutManager(model);
            layout.Update();

            Assert.That(layout.RowCount, Is.EqualTo(1));

            Assert.That(ctx1nodeA.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
            Assert.That(ctx1nodeB.Position.X, Is.EqualTo(ctx1nodeA.Position.X + ctx1nodeA.Allocation.Width + layout.XPadding));
            Assert.That(ctx1nodeB.Position.Y, Is.EqualTo(layout.YMargin));
            Assert.That(ctx2node.Position.X, Is.EqualTo(ctx1nodeB.Position.X + ctx1nodeB.Allocation.Width + layout.XPadding));
            Assert.That(ctx2node.Position.Y, Is.EqualTo(layout.YMargin));
        }

        [Test()]
        public void TwoContextsOverlappingNodes()
        {
            TestModel model = new TestModel();
            string context1 = "Context1";
            INode ctx1nodeA = model.CreateAddNode(10, context1, new Size(80, 50));
            INode ctx1nodeB = model.CreateAddNode(20, context1, new Size(100, 40));
            string context2 = "Context2";
            INode ctx2node = model.CreateAddNode(15, context2, new Size(50, 50));

            TimelineLayoutManager layout = new TimelineLayoutManager(model);
            layout.Update();

            Assert.That(layout.RowCount, Is.EqualTo(2));

            Assert.That(ctx1nodeA.Position, Is.EqualTo(new Point(layout.XMargin, layout.YMargin)));
            Assert.That(ctx1nodeB.Position.X, Is.EqualTo(layout.XMargin + ctx1nodeA.Allocation.Width + layout.XPadding + ctx2node.Allocation.Width + layout.XPadding));
            Assert.That(ctx1nodeB.Position.Y, Is.EqualTo(layout.YMargin));

            Assert.That(ctx2node.Position.X, Is.EqualTo(ctx1nodeA.Position.X + ctx1nodeA.Allocation.Width + layout.XPadding));
            Assert.That(ctx2node.Position.Y, Is.EqualTo(layout.YMargin + 50 + layout.YPadding));
        }
    }
}