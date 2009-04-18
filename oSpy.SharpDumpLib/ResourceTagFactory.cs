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

namespace oSpy.SharpDumpLib
{
    public abstract class ResourceTagFactory : ITagFactory
    {
        protected Dictionary<uint, ResourceTag> m_tags = new Dictionary<uint, ResourceTag>();

        public ITag[] GetTags(Event ev)
        {
            ResourceTag tag;

            uint handle = GetResourceHandleFromEvent(ev);
            if (IsResourceAllocationEvent(ev) || !m_tags.ContainsKey(handle))
            {
                tag = CreateResourceTag(handle);
                m_tags[handle] = tag;
            }
            else
            {
                tag = m_tags[handle];
            }

            return new ITag[] { tag };
        }

        protected abstract uint GetResourceHandleFromEvent(Event ev);
        protected abstract bool IsResourceAllocationEvent(Event ev);
        protected abstract ResourceTag CreateResourceTag(uint handle);
    }
}
