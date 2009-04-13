//
// Copyright (c) 2009 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Net.Sockets;

namespace oSpy.SharpDumpLib.Socket
{
    public class ConnectEvent : Event
    {
        public ConnectEvent (EventInformation eventInformation)
            : base (eventInformation)
        {
        }
    }

    [FunctionCallEventFactory ("connect")]
    public class ConnectEventFactory : SpecificEventFactory
    {
        public Event CreateEvent (EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            return new ConnectEvent (eventInformation);
        }
    }
}
