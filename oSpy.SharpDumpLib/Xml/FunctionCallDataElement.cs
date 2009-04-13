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
using System.Xml;

namespace oSpy.SharpDumpLib
{
    public class FunctionCallDataElement
    {
        private XmlElement data_element;

        public string FirstArgument {
            get { return data_element.SelectSingleNode ("/data/arguments[@direction='in']/argument[1]/value/@value").InnerText; }
        }

        public string SecondArgument {
            get { return data_element.SelectSingleNode ("/data/arguments[@direction='in']/argument[2]/value/@value").InnerText; }
        }

        public string ThirdArgument {
            get { return data_element.SelectSingleNode ("/data/arguments[@direction='in']/argument[3]/value/@value").InnerText; }
        }

        public uint ReturnValueAsUInt {
            get {
                string str = data_element.SelectSingleNode ("/data/returnValue/value/@value").InnerText;
                if (str.StartsWith ("0x"))
                    return Convert.ToUInt32 (str, 16);
                else
                    return Convert.ToUInt32 (str);
            }
        }

        public FunctionCallDataElement(XmlElement dataElement)
        {
            this.data_element = dataElement;
        }
    }
}
