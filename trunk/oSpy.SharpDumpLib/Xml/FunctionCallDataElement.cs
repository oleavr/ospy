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
using System.Xml;

namespace oSpy.SharpDumpLib
{
    internal class FunctionCallDataElement
    {
        private XmlElement m_dataElement;

        public string FirstArgument
        {
            get
            {
                return GetSimpleArgumentValueAsString(1);
            }
        }

        public string SecondArgument
        {
            get
            {
                return GetSimpleArgumentValueAsString(2);
            }
        }

        public string ThirdArgument
        {
            get
            {
                return GetSimpleArgumentValueAsString(3);
            }
        }

        public string ReturnValue
        {
            get
            {
                return m_dataElement.SelectSingleNode("/event/returnValue/value/@value").InnerText.Trim();
            }
        }

        public int ReturnValueAsInt
        {
            get
            {
                return ConvertStringToInt(ReturnValue);
            }
        }

        public uint ReturnValueAsUInt
        {
            get
            {
                return ConvertStringToUInt(ReturnValue);
            }
        }

        public int LastError
        {
            get
            {
                return Convert.ToInt32(m_dataElement.SelectSingleNode("/event/lastError/@value").Value);
            }
        }

        public FunctionCallDataElement(XmlElement dataElement)
        {
            m_dataElement = dataElement;
        }

        public string GetSimpleArgumentValueAsString(uint n)
        {
            return m_dataElement.SelectSingleNode("/event/arguments[@direction='in']/argument[" + n + "]/value/@value").InnerText.Trim();
        }

        public int GetSimpleArgumentValueAsInt(uint n)
        {
            return ConvertStringToInt(GetSimpleArgumentValueAsString(n));
        }

        public uint GetSimpleArgumentValueAsUInt(uint n)
        {
            return ConvertStringToUInt(GetSimpleArgumentValueAsString(n));
        }

        private int ConvertStringToInt(string str)
        {
            if (str.StartsWith("0x"))
                return Convert.ToInt32(str, 16);
            else
                return Convert.ToInt32(str);
        }

        private uint ConvertStringToUInt(string str)
        {
            if (str.StartsWith("0x"))
                return Convert.ToUInt32(str, 16);
            else
                return Convert.ToUInt32(str);
        }
    }
}
