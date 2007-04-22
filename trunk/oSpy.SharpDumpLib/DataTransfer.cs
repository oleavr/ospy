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
    public class DataTransfer : IMetadata
    {
        private Resource resource = null;
        public Resource Resource
        {
            get { return resource; }
        }

        private BulkSlot slot = null;
        public byte[] Data
        {
            get { return slot.Data; }
        }
        
        public int Size
        {
            get { return slot.Size; }
        }

        private DataDirection direction;
        public DataDirection Direction
        {
            get { return direction; }
        }

        private uint eventId = 0;
        public uint EventId
        {
            get { return eventId; }
        }
        
        private string functionName = null;
        public string FunctionName
        {
            get { return functionName; }
        }

        private Dictionary<string, object> metadata = new Dictionary<string, object>();

        public DataTransfer(Resource resource, BulkSlot slot, DataDirection direction, uint eventId, string functionName)
        {
            this.resource = resource;
            this.slot = slot;
            this.direction = direction;            this.eventId = eventId;
            this.functionName = functionName;
        }

        public override string ToString()
        {
        	if (metadata.Count == 1)
        	{
        	    string firstKey = null;
        	    foreach (string key in metadata.Keys)
        	        firstKey = key;

        	    return String.Format("DataExchange [{0} = {1}]", firstKey, metadata[firstKey]);  
        	}
        	else if (metadata.Count == 0)
        	{
        	    return "DataExchange [no metadata]";
        	}
        	else
        	{
        	    return String.Format("DataExchange [{0} metadata keys]", metadata.Count);
        	}
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
