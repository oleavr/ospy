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
    public enum DataDirection
    {
        Unknown,
        Incoming,
        Outgoing,
    }

    [Serializable]
    public abstract class DataTransfer : IMetadata
    {
        public abstract byte[] Data { get; }
        public abstract int Size { get; }

        private DataDirection direction;
        public DataDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        private uint eventId;
        public uint EventId
        {
            get { return eventId; }
            set { eventId = value; }
        }

        private string functionName;
        public string FunctionName
        {
            get { return functionName; }
            set { functionName = value; }
        }

        private Dictionary<string, object> metadata = new Dictionary<string, object> ();

        public DataTransfer (DataDirection direction, uint eventId, string functionName)
        {
            this.direction = direction;
            this.eventId = eventId;
            this.functionName = functionName;
        }

        public DataTransfer (DataTransfer transfer)
        {
            this.direction = transfer.direction;
            this.eventId = transfer.eventId;
            this.functionName = transfer.functionName;
        }

        public override string ToString ()
        {
            if (metadata.Count == 1)
            {
                string firstKey = null;
                foreach (string key in metadata.Keys)
                    firstKey = key;

                return String.Format ("DataTransfer [{0} = {1}]", firstKey, metadata[firstKey]);
            }
            else if (metadata.Count == 0)
            {
                return "DataTransfer [no metadata]";
            }
            else
            {
                return String.Format ("DataTransfer [{0} metadata keys]", metadata.Count);
            }
        }

        #region IMetadata implementation

        public List<string> GetMetaKeys ()
        {
            return new List<string> (metadata.Keys);
        }

        public bool HasMetaKey (string name)
        {
            return metadata.ContainsKey (name);
        }

        public object GetMetaValue (string name)
        {
            return metadata[name];
        }

        public void SetMetaValue (string name, object value)
        {
            metadata[name] = value;
        }

        #endregion // IMetadata implementation
    }

    public class CompactDataTransfer : DataTransfer
    {
        private BulkSlot slot = null;

        public override byte[] Data
        {
            get { return slot.Data; }
        }

        public override int Size
        {
            get { return slot.Size; }
        }

        public CompactDataTransfer (DataDirection direction, uint eventId, string functionName, BulkSlot slot)
            : base (direction, eventId, functionName)
        {
            this.slot = slot;
        }
    }

    [Serializable]
    public class MemoryDataTransfer : DataTransfer
    {
        public byte[] data;
        public override byte[] Data
        {
            get { return data; }
        }

        public override int Size
        {
            get { return data.Length; }
        }

        public MemoryDataTransfer ()
            : base (DataDirection.Unknown, 0, String.Empty)
        {
            this.data = new byte [0];
        }

        public MemoryDataTransfer (DataDirection direction, uint eventId, string functionName, byte[] data)
            : base (direction, eventId, functionName)
        {
            this.data = data;
        }

        public MemoryDataTransfer (DataTransfer transfer)
            : base (transfer)
        {
            this.data = transfer.Data;
        }
    }
}
