//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Collections.Generic;

namespace oSpy.SharpDumpLib.Socket
{
    [TagFactoryFor(typeof(CreateEvent))]
    [TagFactoryFor(typeof(ConnectEvent))]
    public class TagFactory : ResourceTagFactory
    {
        protected override uint GetResourceHandleFromEvent(Event ev)
        {
            if (ev is CreateEvent)
            {
                CreateEvent createEvent = ev as CreateEvent;
                return createEvent.Result;
            }
            else if (ev is ConnectEvent)
            {
                ConnectEvent connEvent = ev as ConnectEvent;
                return connEvent.Socket;
            }
            else
            {
                throw new NotImplementedException("Should not get here");
            }
        }

        protected override bool IsResourceAllocationEvent(Event ev)
        {
            return (ev is CreateEvent);
        }

        protected override ResourceTag CreateResourceTag(uint handle)
        {
            return new SocketResourceTag(handle);
        }
    }
}
