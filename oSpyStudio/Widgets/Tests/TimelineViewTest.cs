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
    public class TimelineViewTest
    {
        [Test()]
        public void OneContextTwoNodes()
        {
            Gtk.Application.Init ();

            string context = "Context1";
            TestModel model = new TestModel();
            /*INode nodeA = */model.CreateAddNode(10, context, new Size(80, 50));
            /*INode nodeB = */model.CreateAddNode(20, context, new Size(100, 40));
            TimelineLayoutManager layout = new TimelineLayoutManager(model);
            layout.Update();

            TestRenderer renderer = new TestRenderer();
            TimelineView view = new TimelineView(model, renderer);

            Gtk.Widget[] children = view.Children;

            Assert.That(children.Length, Is.EqualTo(2));

            Assert.That(children[0].Visible, Is.True);
            Assert.That(children[1].Visible, Is.True);

            int ax = (int) view.ChildGetProperty(children[0], "x").Val;
            int ay = (int) view.ChildGetProperty(children[0], "y").Val;
            int bx = (int) view.ChildGetProperty(children[1], "x").Val;
            int by = (int) view.ChildGetProperty(children[1], "y").Val;

            Assert.That(ay, Is.GreaterThan(0));
            Assert.That(ay, Is.EqualTo(by));

            Assert.That(ax, Is.GreaterThan(0));
            Assert.That(bx, Is.GreaterThan(0));
            Assert.That(ax, Is.Not.EqualTo(bx));
        }
    }
}