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
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Data;
using System.Windows.Forms;

namespace oSpy
{
    public enum MessageType
    {
        MESSAGE_TYPE_MESSAGE = 0,
        MESSAGE_TYPE_PACKET = 1,
    };

    public enum MessageContext
    {
        MESSAGE_CTX_INFO = 0,
        MESSAGE_CTX_WARNING = 1,
        MESSAGE_CTX_ERROR = 2,
        MESSAGE_CTX_SOCKET_LISTENING = 3,
        MESSAGE_CTX_SOCKET_CONNECTING = 4,
        MESSAGE_CTX_SOCKET_CONNECTED = 5,
        MESSAGE_CTX_SOCKET_DISCONNECTED = 6,
        MESSAGE_CTX_SOCKET_RESET = 7,
        MESSAGE_CTX_ACTIVESYNC_DEVICE = 8,
        MESSAGE_CTX_ACTIVESYNC_STATUS = 9,
        MESSAGE_CTX_ACTIVESYNC_SUBSTATUS = 10,
        MESSAGE_CTX_ACTIVESYNC_WZ_STATUS = 11,
    };

    public enum PacketDirection
    {
        PACKET_DIRECTION_INVALID = 0,
        PACKET_DIRECTION_INCOMING = 1,
        PACKET_DIRECTION_OUTGOING = 2,
    };
}
