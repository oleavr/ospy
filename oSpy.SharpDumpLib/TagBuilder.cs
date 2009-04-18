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
using System.Reflection;

namespace oSpy.SharpDumpLib
{
    public class TagBuilder
    {
        private Dictionary<Type, ITagFactory> m_factories = new Dictionary<Type, ITagFactory>();

        public TagBuilder()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (Type t in asm.GetTypes())
            {
                object[] attrs = t.GetCustomAttributes(typeof(TagFactoryForAttribute), false);
                if (attrs.Length == 0)
                    continue;

                ConstructorInfo[] ctors = t.GetConstructors();
                ITagFactory factory = ctors[0].Invoke(new object[] { }) as ITagFactory;
                foreach (TagFactoryForAttribute attr in attrs)
                    m_factories[attr.EventType] = factory;
            }
        }

        public void Process(Event ev)
        {
            ITagFactory factory;
            if (m_factories.TryGetValue(ev.GetType(), out factory))
            {
                foreach (ITag tag in factory.GetTags(ev))
                    ev.Tags.Add(tag);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class TagFactoryForAttribute : Attribute
    {
        private Type m_eventType;

        public Type EventType
        {
            get
            {
                return m_eventType;
            }
        }

        public TagFactoryForAttribute(Type eventType)
        {
            m_eventType = eventType;
        }
    }
}
