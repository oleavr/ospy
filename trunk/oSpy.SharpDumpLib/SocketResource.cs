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
   			return String.Format("0x{0:x} socket with af={1}, type={2}", handle, addressFamily, socketType);
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
