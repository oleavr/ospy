//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

namespace oSpy.SharpDumpLib
{
    public class DataExchange : IMetadata
    {
        private Resource resource = null;
        public Resource Resource
        {
            get { return resource; }
        }

        private BulkStorage storage = null;

        private List<BulkSlot> slots = new List<BulkSlot>();
        private List<DataDirection> directions = new List<DataDirection>();

        private Dictionary<string, object> metadata = new Dictionary<string, object>();

        public DataExchange(Resource resource)
        {
            this.resource = resource;
        }

        public DataExchange(Resource resource, BulkSlot resourceSlot, DataDirection direction)
        {
            this.resource = resource;

            this.slots.Add(resourceSlot);
            this.directions.Add(direction);
        }

        public void Close()
        {
            slots.Clear();
            directions.Clear();

            if (storage != null)
            {
                storage.Close();
                storage = null;
            }
        }

        public void Append(byte[] data, DataDirection direction)
        {
            if (storage == null)
            {
                storage = new BulkStorage();
            }

            BulkSlot slot = storage.AppendData(data);
            slots.Add(slot);
            directions.Add(direction);
        }

        #region IMetadata implementation

        public List<string> GetMetaKeys()
        {
            return new List<string>(metadata.Keys);
        }

        public bool HasMetaKey(string name)
        {
            return metadata.ContainsKey(name);
        }

        public object GetMetaValue(string name)
        {
            return metadata[name];
        }

        public void SetMetaValue(string name, object value)
        {
            metadata[name] = value;
        }

        #endregion // IMetadata implementation
    }

    public enum DataDirection
    {
        Unknown,
        Incoming,
        Outgoing,
    }
}
