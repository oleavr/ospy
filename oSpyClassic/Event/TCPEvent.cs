/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
using System;
using System.Collections.Generic;
using System.Text;
using oSpyClassic.Parser;
using oSpyClassic.Net;
namespace oSpyClassic.Event
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
