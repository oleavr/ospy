//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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
    public class Process
    {
        private uint id;
        public uint Id
        {
            get { return id; }
        }

        private string name;
        public string Name
        {
            get { return name; }
        }

        private List<Resource> resources = new List<Resource>();
        public List<Resource> Resources
        {
            get { return resources; }
        }

        public Process(uint id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public void Close()
        {
            foreach (Resource res in resources)
            {
                res.Close();
            }
            resources.Clear();
        }
        
        public override string ToString()
        {
            return String.Format("{0} [{1}]", name, id);
        }
    }
}
