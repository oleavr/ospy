//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

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
