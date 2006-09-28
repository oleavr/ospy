using System;
using System.Collections.Generic;
using System.Text;
using Flobbster.Windows.Forms;
using System.ComponentModel;
using oSpy.Net;
namespace oSpy.Parser
{
    [Serializable()]
    [TypeConverterAttribute(typeof(TransactionNodeConverter))]
    public class TransactionNode : PropertyTable, IComparable
    {
        protected TransactionNode parent;
        public TransactionNode Parent
        {
            get { return parent; }
        }

        protected List<TransactionNode> children;
        public List<TransactionNode> Children
        {
            get { return children; }
        }

        protected Dictionary<string, TransactionNode> childrenDict;
        public Dictionary<string, TransactionNode> ChildrenDict
        {
            get { return childrenDict; }
        }

        protected string name;
        public string Name
        {
            get { return name; }
        }

        protected string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        protected string summary;
        public string Summary
        {
            get { return summary; }
            set { summary = value; }
        }

        protected Dictionary<string, object> fields;
        public Dictionary<string, object> Fields
        {
            get { return fields; }
        }

        protected List<PacketSlice> slices;
        public List<PacketSlice> Slices
        {
            get { return slices; }
        }

        protected Dictionary<string, List<PacketSlice>> fieldSlices;

        public PacketSlice FirstSlice
        {
            get
            {
                if (slices.Count > 0)
                    return slices[0];
                else if (children.Count > 0)
                    return children[0].FirstSlice;
                else
                    return null;
            }
        }

        public PacketSlice LastSlice
        {
            get
            {
                if (children.Count > 0)
                    return children[children.Count - 1].LastSlice;
                else if (slices.Count > 0)
                    return slices[slices.Count - 1];
                else
                    return null;
            }
        }

        public DateTime StartTime
        {
            get { return FirstSlice.Packet.Timestamp; }
        }

        public DateTime EndTime
        {
            get { return LastSlice.Packet.Timestamp; }
        }

        public int Index
        {
            get
            {
                PacketSlice slice = FirstSlice;
                return (slice.Packet.Index << 11) | slice.Offset;
            }
        }

        public TransactionNode(string name)
        {
            Initialize(null, name);
        }

        public TransactionNode(TransactionNode parent, string name)
        {
            Initialize(parent, name);

            parent.AddChild(this);
        }

        private void Initialize(TransactionNode parent, string name)
        {
            this.name = name;
            this.description = "";
            this.summary = "";
            this.parent = parent;
            this.children = new List<TransactionNode>();
            this.childrenDict = new Dictionary<string, TransactionNode>();
            this.slices = new List<PacketSlice>();
            this.fields = new Dictionary<string, object>();
            this.fieldSlices = new Dictionary<string, List<PacketSlice>>();
        }

        public void AddChild(TransactionNode node)
        {
            children.Add(node);
            childrenDict[node.Name] = node;

            PropertySpec propSpec = new PropertySpec(node.Name, typeof(TransactionNode), "Packet");
            propSpec.Description = node.Name;
            propSpec.Attributes = new Attribute[1] { new ReadOnlyAttribute(true) };
            Properties.Add(propSpec);
            this[node.Name] = node;
        }

        public TransactionNode FindChild(string name)
        {
            if (childrenDict.ContainsKey(name))
                return childrenDict[name];

            foreach (TransactionNode child in childrenDict.Values)
            {
                TransactionNode node = child.FindChild(name);
                if (node != null)
                    return node;
            }

            return null;
        }

        public void AddField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddField(name, value, value, description, slices);
        }

        public void AddField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            this.fields[name] = value;
            Properties.Add(new PropertySpec(name, typeof(ValueType), "Packet", description));
            this[name] = formattedValue;
            AddFieldSlices(name, slices);
        }

        public void AddTextField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddTextField(name, value, value, description, slices);
        }

        public void AddTextField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            AddSpecialField(name, value, formattedValue, description, slices, typeof(TextUITypeEditor));
        }

        public void AddXMLField(string name, object value, string description, List<PacketSlice> slices)
        {
            AddXMLField(name, value, value, description, slices);
        }

        public void AddXMLField(string name, object value, object formattedValue, string description, List<PacketSlice> slices)
        {
            AddSpecialField(name, value, formattedValue, description, slices, typeof(XMLUITypeEditor));
        }

        protected void AddSpecialField(string name, object value, object formattedValue, string description, List<PacketSlice> slices, Type editorType)
        {
            this.fields[name] = value;
            PropertySpec propSpec = new PropertySpec(name, typeof(string), "Packet");
            propSpec.Description = description;
            propSpec.Attributes = new Attribute[2] {
                    new System.ComponentModel.EditorAttribute(editorType, typeof(System.Drawing.Design.UITypeEditor)),
                    new ReadOnlyAttribute(true)
                };
            Properties.Add(propSpec);
            this[name] = formattedValue;
            AddFieldSlices(name, slices);
        }

        protected void AddFieldSlices(string name, List<PacketSlice> slices)
        {
            this.slices.AddRange(slices);
            fieldSlices[name] = new List<PacketSlice>(slices);
            slices.Clear();
        }

        public List<PacketSlice> GetAllSlices()
        {
            List<PacketSlice> all = new List<PacketSlice>(slices.Count);
            all.AddRange(slices);

            foreach (TransactionNode node in childrenDict.Values)
            {
                List<PacketSlice> childAll = node.GetAllSlices();
                all.AddRange(childAll);
            }

            return all;
        }

        //
        // Gets the slices for the given path, which may contain the
        // full pipe-separated path to a subnode's field, or just the
        // name of a field on this node.
        //
        // For example:
        //   "1|Request|hKey"
        //
        public List<PacketSlice> GetSlicesForFieldPath(string path)
        {
            if (path == null || path == "")
                return GetAllSlices();

            if (path.IndexOf("|") == -1)
            {
                if (childrenDict.ContainsKey(path))
                    return childrenDict[path].GetAllSlices();
                else
                    return fieldSlices[path];
            }

            string[] tokens = path.Split(new char[] { '|' }, 2);
            return childrenDict[tokens[0]].GetSlicesForFieldPath(tokens[1]);
        }

        public int CompareTo(Object obj)
        {
            TransactionNode otherNode = obj as TransactionNode;

            PacketSlice slice = FirstSlice;
            PacketSlice otherSlice = otherNode.FirstSlice;

            if (slice != null && otherSlice != null)
                return FirstSlice.CompareTo(otherNode.FirstSlice);
            else
                return 0; // FIXME
        }
    }
}
