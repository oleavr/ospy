/**
 * Copyright (C) 2007  Ole André Vadla Ravnås <oleavr@gmail.com>
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
using oSpy.Util;
using oSpy.Net;

namespace oSpy.Parser
{
    public class XMPPTransactionFactory : TransactionFactory
    {
        public const int XMPP_PORT = 5222;

        public XMPPTransactionFactory(DebugLogger logger)
            : base(logger)
        {
        }

        public override string Name()
        {
            return "XMPP Transaction Factory";
        }

        public override bool HandleSession(IPSession session)
        {
            if (session.RemoteEndpoint == null || session.RemoteEndpoint.Port != XMPP_PORT)
                return false;

            PacketStream stream = session.GetNextStreamDirection();

            if (stream.PeekStringUTF8(8) != "<stream")
                return false;

            // FIXME

            return false;
        }
    }
}
