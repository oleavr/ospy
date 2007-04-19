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
using System.Text;

namespace oSpy.SharpDumpLib
{
    public class Resource
    {
        private UInt32 handle = 0;
        public UInt32 Handle
        {
            get { return handle; }
        }

        private List<DataExchange> dataExchanges = new List<DataExchange>();
        public List<DataExchange> DataExchanges
        {
            get { return dataExchanges; }
        }

        private BulkStorage storage = null;

        public Resource(UInt32 handle)
        {
            this.handle = handle;
        }

        public void Close()
        {
            foreach (DataExchange exchange in dataExchanges)
            {
                exchange.Close();
            }
            dataExchanges.Clear();

            if (storage != null)
            {
                storage.Close();
                storage = null;
            }
        }

        public void AppendData(byte[] data, DataDirection direction)
        {
            if (DataIsContinuous())
            {
                DataExchange exchange = null;

                if (dataExchanges.Count != 0)
                {
                    exchange = dataExchanges[0];
                }
                else
                {
                    exchange = new DataExchange(this);
                    dataExchanges.Add(exchange);
                }

                exchange.Append(data, direction);
            }
            else
            {
                if (storage == null)
                {
                    storage = new BulkStorage();
                }

                dataExchanges.Add(new DataExchange(this, storage.AppendData(data), direction));
            }
        }

        protected virtual bool DataIsContinuous()
        {
            return true;
        }
    }

    public enum ResourceType
    {
        Unknown,
        Socket,
        CryptoContext,
    }
}
