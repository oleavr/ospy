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

namespace oSpy.SharpDumpLib
{
    public enum EventType
    {
        Debug,
        Info,
        Warning,
        Error,
        FunctionCall,
        AsyncResult,
        IOCTL_INTERNAL_USB_SUBMIT_URB,
    }

    public class Event
    {
        protected EventInformation event_information;

        public uint Id {
            get { return event_information.Id; }
        }

        public EventType Type {
            get { return event_information.Type; }
        }

        public DateTime Timestamp {
            get { return event_information.Timestamp; }
        }

        public string ProcessName {
            get { return event_information.ProcessName; }
        }

        public uint ProcessId {
            get { return event_information.ProcessId; }
        }

        public uint ThreadId {
            get { return event_information.ThreadId; }
        }

        public string Data {
            get { return event_information.Data; }
        }

        public Event (EventInformation eventInformation)
        {
            this.event_information = eventInformation;
        }
    }

    public struct EventInformation
    {
        public uint Id;
        public EventType Type;
        public DateTime Timestamp;
        public string ProcessName;
        public uint ProcessId;
        public uint ThreadId;
        public string Data;
    }
}
