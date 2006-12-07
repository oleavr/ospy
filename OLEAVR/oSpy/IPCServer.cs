using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace oSpy
{
    class IPCServer
    {
        protected enum enumFileMap : uint
        {
            FILE_MAP_READ = 0x4,
            FILE_MAP_WRITE = 0x2,
            FILE_MAP_COPY = 0x1,
            FILE_MAP_ALL_ACCESS = 0x1 + 0x2 + 0x4 + 0x8 + 0x10 + 0xF0000
        };

        protected enum enumProtect : uint
        {
            PAGE_NOACCESS = 0x1,
            PAGE_READONLY = 0x2,
            PAGE_READWRITE = 0x4,
            PAGE_WRITECOPY = 0x8,
            PAGE_EXECUTE = 0x10
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

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern SafeWaitHandle CreateEvent(IntPtr lpEventAttributes, bool bManualReset,
                                                           bool bInitialState, string lpName);


        public const int IPC_BLOCK_COUNT = 512;
        public const int IPC_BLOCK_SIZE = 4096;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct Block
        {
            public UInt32 Next;
            public UInt32 Prev;

            public volatile UInt32 DoneRead;
            public volatile UInt32 DoneWrite;

            public UInt32 Amount;
            public UInt32 _Padding;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = IPC_BLOCK_SIZE)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct MemBuff
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = IPC_BLOCK_COUNT)]
            public Block[] Blocks;

            public volatile UInt32 ReadEnd;
            public volatile UInt32 ReadStart;

            public volatile UInt32 WriteEnd;
            public volatile UInt32 WriteStart;
        }

        private AutoResetEvent evtFilled, evtAvail;
        private IntPtr mapFile;

        public IPCServer()
        {
            string evtFilledName = "oSpy_evtFilled";
            string evtAvailName = "oSpy_evtAvail";
            string memName = "oSpy_mem";

            evtFilled = new AutoResetEvent(false);
            evtFilled.SafeWaitHandle = CreateEvent(IntPtr.Zero, false, false, evtFilledName);

            evtAvail = new AutoResetEvent(false);
            evtFilled.SafeWaitHandle = CreateEvent(IntPtr.Zero, false, false, evtAvailName);

            mapFile = CreateFileMapping(0xFFFFFFFFu, IntPtr.Zero, enumProtect.PAGE_READWRITE,
                                        0, (uint)Marshal.SizeOf(typeof(MemBuff)), memName);


        }
    }
}
