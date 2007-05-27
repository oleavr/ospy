//
// Copyright (c) 2006 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
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
