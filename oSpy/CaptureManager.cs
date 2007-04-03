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
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace oSpy
{
    public class CaptureError : Exception
    {
        public CaptureError(string msg)
            : base(msg)
        {
        }
    }

    public class CaptureManager
    {
        protected const int MAX_PATH = 260;
        protected const int ERROR_ALREADY_EXISTS = 183;

        protected enum enumProtect : uint
        {
            PAGE_NOACCESS = 0x1,
            PAGE_READONLY = 0x2,
            PAGE_READWRITE = 0x4,
            PAGE_WRITECOPY = 0x8,
            PAGE_EXECUTE = 0x10
        };
        protected enum enumFileMap : uint
        {
            FILE_MAP_READ = 0x4,
            FILE_MAP_WRITE = 0x2,
            FILE_MAP_COPY = 0x1,
            FILE_MAP_ALL_ACCESS = 0x1 + 0x2 + 0x4 + 0x8 + 0x10 + 0xF0000
        };

        [DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping",
            SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern IntPtr CreateFileMapping(uint hFile,
          IntPtr lpAttributes, enumProtect flProtect,
          uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("Kernel32.dll", EntryPoint = "MapViewOfFile",
            SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
          enumFileMap dwDesiredAccess, uint dwFileOffsetHigh,
          uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        public const int MAX_SOFTWALL_RULES = 128;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct SoftwallRule
        {
            /* mask of conditions */
            public Int32 conditions;

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

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
        public struct CaptureConfig
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string LogPath;
            public volatile UInt32 LogIndex;

            public UInt32 NumSoftwallRules;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SOFTWALL_RULES)]
            public SoftwallRule[] SoftwallRules;
        }

        public CaptureManager()
        {
        }

        public void StartCapture()
        {
            IntPtr map = CreateFileMapping(0xFFFFFFFFu, IntPtr.Zero,
                                           enumProtect.PAGE_READWRITE,
                                           0, (uint)Marshal.SizeOf(typeof(CaptureConfig)),
                                           "oSpyCaptureConfig");
            if (Marshal.GetLastWin32Error() == ERROR_ALREADY_EXISTS)
                throw new CaptureError("Is another instance of oSpy or one or more processes previously monitored still alive?");

            IntPtr cfgPtr = MapViewOfFile(map, enumFileMap.FILE_MAP_WRITE, 0, 0, (uint)Marshal.SizeOf(typeof(CaptureConfig)));

            // Create a temporary directory for the capture
            string tmpDir;
            do
            {
                tmpDir = String.Format("{0}{1}", Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(tmpDir));

            Directory.CreateDirectory(tmpDir);

            // Write the temporary directory to shared memory
            char[] tmpDirChars = tmpDir.ToCharArray();
            IntPtr ptr = (IntPtr) (cfgPtr.ToInt64() + Marshal.OffsetOf(typeof(CaptureConfig), "LogPath").ToInt64());
            Marshal.Copy(tmpDirChars, 0, ptr, tmpDirChars.Length);

            // And make it NUL-terminated
            ptr = (IntPtr) (ptr.ToInt64() + (tmpDirChars.Length * Marshal.SizeOf(typeof(UInt16))));
            Marshal.WriteInt16(ptr, 0);
        }
    }
}
