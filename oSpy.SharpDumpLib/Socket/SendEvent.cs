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
    public class SendEvent : Event
    {
        private uint socket;
        public uint Socket {
            get { return socket; }
        }

        private byte[] buffer;
        public byte[] Buffer {
            get { return buffer; }
        }

        private int flags;
        public int Flags {
            get { return flags; }
        }

        private int result;
        public int Result {
            get { return result; }
        }

        public SendEvent (EventInformation eventInformation, uint socket,
                          byte[] buffer, int flags, int result)
            : base (eventInformation)
        {
            this.socket = socket;
            this.buffer = buffer;
            this.flags = flags;
            this.result = result;
        }
    }

    [FunctionCallEventFactory ("send")]
    public class SendEventFactory : SpecificEventFactory
    {
        public Event CreateEvent (EventInformation eventInformation, System.Xml.XmlElement eventData)
        {
            FunctionCallDataElement el = new FunctionCallDataElement (eventData);

            uint socket = el.GetSimpleArgumentValueAsUInt (1);

            string buffer_base64 = eventData.SelectSingleNode ("/event/arguments[@direction='in']/argument[2]/value/value").InnerText.Trim ();
            byte[] buffer = Convert.FromBase64String (buffer_base64);

            int flags = el.GetSimpleArgumentValueAsInt (4);

            int result = el.ReturnValueAsInt;

            return new SendEvent (eventInformation, socket, buffer, flags, result);
        }
    }
}
