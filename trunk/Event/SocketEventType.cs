using System;
using System.Collections.Generic;
using System.Text;

namespace oSpy.Event
{
    public enum SocketEventType
    {
        UNKNOWN,
        LISTENING,
        CONNECTED_INBOUND,
        CONNECTING,
        CONNECTED_OUTBOUND,
        DISCONNECTED,
        RESET,
    }
}
