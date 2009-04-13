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
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Reflection;

namespace oSpy.SharpDumpLib
{
    public class EventFactory
    {
        private Dictionary<string, SpecificEventFactory> function_call_event_factories = new Dictionary<string, SpecificEventFactory> ();

        public EventFactory ()
        {
            Assembly asm = Assembly.GetCallingAssembly ();
            foreach (Type t in asm.GetTypes ()) {
                object[] attrs = t.GetCustomAttributes (typeof (FunctionCallEventFactoryAttribute), true);
                if (attrs.Length == 0)
                    continue;
                FunctionCallEventFactoryAttribute attr = attrs[0] as FunctionCallEventFactoryAttribute;
                ConstructorInfo[] ctors = t.GetConstructors ();
                SpecificEventFactory eventFactory = ctors[0].Invoke (new object[] {}) as SpecificEventFactory;
                function_call_event_factories[attr.FunctionName] = eventFactory;
            }
        }

        public Event CreateEvent (EventInformation eventInformation)
        {
            SpecificEventFactory sf = null;
            XmlElement eventData = null;
            
            if (eventInformation.Type == EventType.FunctionCall) {
                XmlDocument doc = new XmlDocument ();
                doc.LoadXml (eventInformation.Data);
                eventData = doc.DocumentElement;

                string fullFunctionName = eventData.SelectSingleNode ("/data/name").InnerText.Trim ();
                string functionName = fullFunctionName.Split (new string[] { "::" }, StringSplitOptions.None)[1];
                function_call_event_factories.TryGetValue (functionName, out sf);
            }

            if (sf != null)
                return sf.CreateEvent (eventInformation, eventData);
            else
                return new Event (eventInformation);
        }
        
        public static Event CreateFromXml (string xml)
        {            
            XmlReader reader = new XmlTextReader (new StringReader (xml));
            reader.Read ();
            reader = reader.ReadSubtree ();
            XmlDocument doc = new XmlDocument ();
            doc.Load (reader);

            XmlElement el = doc.DocumentElement;
            XmlAttributeCollection attrs = el.Attributes;

            EventInformation event_information = new EventInformation ();
            event_information.Id = Convert.ToUInt32 (attrs["id"].Value);
            event_information.Type = (EventType) Enum.Parse (typeof (EventType), attrs["type"].Value);
            event_information.Timestamp = DateTime.FromFileTimeUtc (Convert.ToInt64 (attrs["timestamp"].Value));
            event_information.ProcessName = attrs["processName"].Value;
            event_information.ProcessId = Convert.ToUInt32 (attrs["processId"].Value);
            event_information.ThreadId = Convert.ToUInt32 (attrs["threadId"].Value);
            event_information.Data = "<data>" + el.InnerXml + "</data>";

            EventFactory factory = new EventFactory ();
            return factory.CreateEvent (event_information);
        }
    }

    public interface SpecificEventFactory
    {
        Event CreateEvent (EventInformation eventInformation, XmlElement eventData);
    }

    [AttributeUsage (AttributeTargets.Class)]
    public class FunctionCallEventFactoryAttribute : Attribute
    {
        private string functionName;
        public string FunctionName {
            get { return functionName; }
        }

        public FunctionCallEventFactoryAttribute (string functionName)
        {
            this.functionName = functionName;
        }
    }
}
