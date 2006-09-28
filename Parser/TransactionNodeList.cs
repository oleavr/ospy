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
