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

namespace oSpy.SharpDumpLib.Socket
{
    public class CloseEvent : Event
    {
        private uint socket;
        public uint Socket {
            get { return socket; }
        }

        private int result;
        public int Result {
            get { return result; }
        }

        public CloseEvent (EventInformation eventInformation, uint socket,
                           int result)
            : base (eventInformation)
        {
            this.socket = socket;
            this.result = result;
        }
    }

    [FunctionCallEventFactory ("closesocket")]
    public class CloseEventFactory : SpecificEventFactory
    {
        public Event CreateEvent (EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            FunctionCallDataElement el = new FunctionCallDataElement (eventData);

            uint socket = el.GetSimpleArgumentValueAsUInt (1);

            int result = el.ReturnValueAsInt;

            return new CloseEvent (eventInformation, socket, result);
        }
    }
}
