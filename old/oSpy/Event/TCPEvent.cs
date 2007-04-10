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
using oSpy.Parser;
using oSpy.Net;
namespace oSpy.Event
{
    [Serializable()]
    public class TCPEvent : IComparable
    {
        protected DateTime timestamp;
        public DateTime Timestamp
        {
            get { return timestamp; }
        }

        protected UInt32 resourceId;
        public UInt32 ResourceId
        {
            get { return resourceId; }
        }

        protected SocketEventType type;
        public SocketEventType Type
        {
            get { return type; }
        }

        protected IPEndpoint localEndpoint;
        public IPEndpoint LocalEndpoint
        {
            get { return localEndpoint; }
        }

        protected IPEndpoint remoteEndpoint;
        public IPEndpoint RemoteEndpoint
        {
            get { return remoteEndpoint; }
        }

        public TCPEvent(DateTime timestamp, UInt32 resourceId, SocketEventType type,
                        IPEndpoint localEndpoint, IPEndpoint remoteEndpoint)
        {
            this.timestamp = timestamp;
            this.resourceId = resourceId;
            this.type = type;
            this.localEndpoint = localEndpoint;
            this.remoteEndpoint = remoteEndpoint;
        }

        public int CompareTo(Object obj)
        {
            TCPEvent otherEvent = obj as TCPEvent;

            return timestamp.CompareTo(otherEvent.timestamp);
        }

        public override string ToString()
        {
            string text;

            switch (type)
            {
                case SocketEventType.CONNECTED_INBOUND:
                    text = "Client connected";
                    break;
                case SocketEventType.CONNECTED_OUTBOUND:
                    text = "Connected";
                    break;
                case SocketEventType.CONNECTING:
                    text = "Connecting";
                    break;
                case SocketEventType.DISCONNECTED:
                    text = "Connection closed";
                    break;
                case SocketEventType.LISTENING:
                    text = "Listening for connections";
                    break;
                case SocketEventType.RESET:
                    text = "Connection reset";
                    break;
                default:
                    text = "Unknown event";
                    break;
            }

            return text;
        }
    }
}
