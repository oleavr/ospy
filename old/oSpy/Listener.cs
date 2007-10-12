//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
