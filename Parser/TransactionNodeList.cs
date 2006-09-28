/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.Text;
using Flobbster.Windows.Forms;
namespace oSpy.Parser
{
    [Serializable()]
    public class TransactionNodeList : PropertyTable
    {
        protected List<TransactionNode> pendingNodes;
        protected Dictionary<int, TransactionNode> nodes;

        public int Count
        {
            get { return nodes.Count; }
        }

        public TransactionNode this[int index]
        {
            get { return nodes[index]; }
        }

        public TransactionNodeList()
        {
            pendingNodes = new List<TransactionNode>();
            nodes = new Dictionary<int, TransactionNode>();
        }

        public void PushNode(TransactionNode node)
        {
            pendingNodes.Add(node);
        }

        public void PushedAllNodes()
        {
            pendingNodes.Sort();

            int i = 0;
            foreach (TransactionNode node in pendingNodes)
            {
                string name = Convert.ToString(i);
                Properties.Add(new PropertySpec(name, node.GetType(), "Node", ""));
                this[name] = node;
                nodes[i] = node;

                i++;
            }

            pendingNodes.Clear();
        }

        public bool Contains(TransactionNode node)
        {
            return (nodes.ContainsValue(node) || pendingNodes.Contains(node));
        }
    }
}
