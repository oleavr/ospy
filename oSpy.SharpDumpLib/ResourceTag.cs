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
    public abstract class ResourceTag : ITag
    {
        protected uint m_resourceHandle;

        public abstract string Name
        {
            get;
        }

        public uint ResourceHandle
        {
            get
            {
                return m_resourceHandle;
            }
        }

        public ResourceTag(uint handle)
        {
            m_resourceHandle = handle;
        }
    }
}
