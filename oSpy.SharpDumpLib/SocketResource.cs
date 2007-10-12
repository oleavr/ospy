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

namespace oSpy.SharpDumpLib
{
    public class SocketResource : Resource
    {
        private AddressFamily addressFamily = AddressFamily.Unknown;
        public AddressFamily AddressFamily
        {
            get { return addressFamily; }
        }

        private SocketType socketType = SocketType.Unknown;
        public SocketType SocketType
        {
            get { return socketType; }
        }

        private System.Net.IPEndPoint curRemoteEndpoint = null;

        public SocketResource(UInt32 handle)
            : base(handle)
        {
        }

        public SocketResource(UInt32 handle, AddressFamily addressFamily, SocketType socketType)
            : base(handle)
        {
            this.addressFamily = addressFamily;
            this.socketType = socketType;
        }

        protected override bool DataIsContinuous()
        {
            return (socketType != SocketType.SOCK_DGRAM);
        }

        public override DataTransfer AppendData(byte[] data, DataDirection direction, uint eventId, string functionName)
        {
            DataTransfer transfer = base.AppendData(data, direction, eventId, functionName);

            if (curRemoteEndpoint != null && curRemoteEndpoint.Address != System.Net.IPAddress.Any)
                transfer.SetMetaValue("net.ipv4.remoteEndpoint", curRemoteEndpoint);

            return transfer;
        }

        public void SetCurrentRemoteEndpoint(System.Net.IPEndPoint endpoint)
        {
            curRemoteEndpoint = endpoint;
        }
        
        public override string ToString()
        {
   			return String.Format("<SocketResource Handle=0x{0:x8} AddressFamily={1} SocketType={2}>", handle, addressFamily, socketType);
        }
    }

    public enum AddressFamily
    {
        Unknown,
        AF_APPLETALK,
        AF_BAN,
        AF_CCITT,
        AF_CHAOS,
        AF_DATAKIT,
        AF_DECnet,
        AF_DLI,
        AF_ECMA,
        AF_FIREFOX,
        AF_HYLINK,
        AF_IMPLINK,
        AF_INET,
        AF_INET6,
        AF_IPX,
        AF_ISO,
        AF_LAT,
        AF_NETBIOS,
        AF_PUP,
        AF_SNA,
        AF_UNIX,
        AF_UNKNOWN1,
        AF_UNSPEC,
        AF_VOICEVIEW,
        AF_MAX,
    }

    public enum SocketType
    {
        Unknown,
        SOCK_DGRAM,
        SOCK_RAW,
        SOCK_RDM,
        SOCK_SEQPACKET,
        SOCK_STREAM,
    }
}
