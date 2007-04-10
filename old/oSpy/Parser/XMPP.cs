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
            if (session.RemoteEndpoint.Port != XMPP_PORT)
                return false;

            PacketStream stream = session.GetNextStreamDirection();

            if (stream.PeekStringUTF8(8) != "<stream")
                return false;

            // FIXME

            return false;
        }
    }
}
