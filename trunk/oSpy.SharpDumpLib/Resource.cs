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
    public class Resource
    {
        protected UInt32 handle = 0;
        public UInt32 Handle
        {
            get { return handle; }
        }

        protected List<DataTransfer> dataTransfers = new List<DataTransfer>();
        public List<DataTransfer> DataTransfers
        {
            get { return dataTransfers; }
        }

        private BulkStorage storage = null;

        public Resource(UInt32 handle)
        {
            this.handle = handle;
        }

        public virtual void Close()
        {
            dataTransfers.Clear();

            if (storage != null)
            {
                storage.Close();
                storage = null;
            }
        }

        public virtual DataTransfer AppendData (byte[] data, DataDirection direction, uint eventId, string functionName)
        {
            DataTransfer transfer = null;

            if (storage == null)
                storage = new BulkStorage ();

            transfer = new CompactDataTransfer (direction, eventId, functionName, storage.AppendData (data));
            dataTransfers.Add (transfer);

            return transfer;
        }

        protected virtual bool DataIsContinuous()
        {
            return true;
        }
        
        public override string ToString()
        {
        	return String.Format("<Resource Handle=0x{0:x8>", handle);
        }
    }

    public enum ResourceType
    {
        Unknown,
        Socket,
        Crypto,
    }
}
