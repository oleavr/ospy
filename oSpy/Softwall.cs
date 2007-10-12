//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
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
using System.Runtime.InteropServices;

namespace oSpy
{
    public class Softwall
    {
        public const int CONDITION_PROCESS_NAME = 1;
        public const int CONDITION_FUNCTION_NAME = 2;
        public const int CONDITION_RETURN_ADDRESS = 4;
        public const int CONDITION_LOCAL_ADDRESS = 8;
        public const int CONDITION_LOCAL_PORT = 16;
        public const int CONDITION_PEER_ADDRESS = 32;
        public const int CONDITION_PEER_PORT = 64;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct Rule
        {
            /* mask of conditions */
            public Int32 Conditions;

            /* condition values */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string ProcessName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string FunctionName;
            public UInt32 ReturnAddress;
            public UInt32 LocalAddress;
            public UInt32 LocalPort;
            public UInt32 RemoteAddress;
            public UInt32 RemotePort;

            /* return value and lasterror to set if all conditions match */
            public Int32 Retval;
            public UInt32 LastError;
        }
    }
}
