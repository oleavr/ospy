//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
