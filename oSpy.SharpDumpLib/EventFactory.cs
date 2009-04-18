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
using System.Xml;
using System.IO;
using System.Reflection;

namespace oSpy.SharpDumpLib
{
    public class EventFactory
    {
        private Dictionary<string, ISpecificEventFactory> m_funcCallFactories = new Dictionary<string, ISpecificEventFactory>();

        public EventFactory()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (Type t in asm.GetTypes())
            {
                object[] attrs = t.GetCustomAttributes(typeof(FunctionCallEventFactoryAttribute), true);
                if (attrs.Length == 0)
                    continue;
                FunctionCallEventFactoryAttribute attr = attrs[0] as FunctionCallEventFactoryAttribute;
                ConstructorInfo[] ctors = t.GetConstructors();
                ISpecificEventFactory eventFactory = ctors[0].Invoke(new object[] { }) as ISpecificEventFactory;
                m_funcCallFactories[attr.FunctionName] = eventFactory;
            }
        }

        public Event CreateEvent(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return CreateEvent(doc.DocumentElement);
        }

        public Event CreateEvent(XmlReader reader)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            return CreateEvent(doc.DocumentElement);
        }

        public Event CreateEvent(XmlElement element)
        {
            XmlAttributeCollection attrs = element.Attributes;

            EventInformation info = new EventInformation();
            info.Id = Convert.ToUInt32(attrs["id"].Value);
            info.Type = (EventType)Enum.Parse(typeof(EventType), attrs["type"].Value);
            info.Timestamp = DateTime.FromFileTimeUtc(Convert.ToInt64(attrs["timestamp"].Value));
            info.ProcessName = attrs["processName"].Value;
            info.ProcessId = Convert.ToUInt32(attrs["processId"].Value);
            info.ThreadId = Convert.ToUInt32(attrs["threadId"].Value);
            info.RawData = element.OuterXml;

            return CreateEvent(info);
        }

        public Event CreateEvent(EventInformation eventInfo)
        {
            ISpecificEventFactory specificFactory = null;
            XmlElement eventData = null;

            if (eventInfo.Type == EventType.FunctionCall)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(eventInfo.RawData);
                eventData = doc.DocumentElement;

                string fullFunctionName = eventData.SelectSingleNode("/event/name").InnerText.Trim();
                string functionName = fullFunctionName.Split(new string[] { "::" }, StringSplitOptions.None)[1];
                m_funcCallFactories.TryGetValue(functionName, out specificFactory);
            }

            if (specificFactory != null)
                return specificFactory.CreateEvent(eventInfo, eventData);
            else
                return new Event(eventInfo);
        }

        public static Event CreateFromXml(string xml)
        {
            EventFactory factory = new EventFactory();
            return factory.CreateEvent(xml);
        }
    }

    public interface ISpecificEventFactory
    {
        Event CreateEvent(EventInformation eventInformation, XmlElement eventData);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FunctionCallEventFactoryAttribute : Attribute
    {
        private string m_functionName;

        public string FunctionName
        {
            get
            {
                return m_functionName;
            }
        }

        public FunctionCallEventFactoryAttribute(string functionName)
        {
            m_functionName = functionName;
        }
    }
}
