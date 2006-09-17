/**
 * Copyright (C) 2006  Ole André Vadla Ravnås <oleavr@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Data;

namespace oSpy
{
    public class AgentListener
    {
        public delegate void ElementsReceivedHandler(AgentListener.MessageQueueElement[] elements);
        public event ElementsReceivedHandler MessageElementsReceived;

        public delegate void StoppedHandler();
        public event StoppedHandler Stopped;

        private bool running;

        private SoftwallRule[] rules;

        public AgentListener()
        {
            running = false;

            Stopped += new StoppedHandler(AgentListener_Stopped);
        }

        private void AgentListener_Stopped()
        {
            running = false;
        }

        public void Start(SoftwallRule[] rules)
        {
            if (running)
                throw new Exception("Already running");

            running = true;

            this.rules = rules;

            Thread thread = new Thread(ListenerThread);
            thread.Start();
        }

        public void Stop()
        {
            running = false;
        }

        public const int MAX_ELEMENTS = 2048;
        public const int PACKET_BUFSIZE = 65536;
        public const int MAX_SOFTWALL_RULES = 128;

        public const int ERROR_ALREADY_EXISTS = 183;

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct SYSTEMTIME
        {
            public UInt16 wYear;
            public UInt16 wMonth;
            public UInt16 wDayOfWeek;
            public UInt16 wDay;
            public UInt16 wHour;
            public UInt16 wMinute;
            public UInt16 wSecond;
            public UInt16 wMilliseconds;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct MessageQueueElement
        {
            /* Common fields */
            public SYSTEMTIME time;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string process_name;
            public UInt32 process_id;
            public UInt32 thread_id;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string function_name;
            public UInt32 return_address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string caller_module_name;

            public UInt32 resource_id;

            public MessageType msg_type;

            /* MessageType.Message */
            public MessageContext context;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string message;

            /* MessageType.Packet */
            public PacketDirection direction;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string local_address;
            public UInt32 local_port;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string peer_address;
            public UInt32 peer_port;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = AgentListener.PACKET_BUFSIZE)]
            public byte[] buf;
            public UInt32 len;
        };

        public const int SOFTWALL_CONDITION_PROCESS_NAME   =  1;
        public const int SOFTWALL_CONDITION_FUNCTION_NAME  =  2;
        public const int SOFTWALL_CONDITION_RETURN_ADDRESS =  4;
        public const int SOFTWALL_CONDITION_LOCAL_ADDRESS  =  8;
        public const int SOFTWALL_CONDITION_LOCAL_PORT     = 16;
        public const int SOFTWALL_CONDITION_REMOTE_ADDRESS = 32;
        public const int SOFTWALL_CONDITION_REMOTE_PORT    = 64;

        /* connect() errors */
        public const int WSAEHOSTUNREACH = 10065;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct SoftwallRule
        {
            /* mask of conditions */
            public Int32 conditions;

            /* condition values */
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string process_name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string function_name;
            public UInt32 return_address;
            public UInt32 local_address;
            public UInt32 local_port;
            public UInt32 remote_address;
            public UInt32 remote_port;

            /* return value and lasterror to set if all conditions match */
            public Int32 retval;
            public UInt32 last_error;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
        public struct MessageQueue
        {
            public UInt32 num_softwall_rules;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SOFTWALL_RULES)]
            public SoftwallRule[] softwall_rules;

            public UInt32 num_elements;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_ELEMENTS)]
            public MessageQueueElement[] elements;
        };

        protected enum enumFileMap : uint
        {
            FILE_MAP_READ = 0x4,
            FILE_MAP_WRITE = 0x2,
            FILE_MAP_COPY = 0x1,
            FILE_MAP_ALL_ACCESS = 0x1 + 0x2 + 0x4 + 0x8 + 0x10 + 0xF0000
        };

        protected enum enumStd : int
        {
            STD_INPUT_HANDLE = -10,
            STD_OUTPUT_HANDLE = -11,
            STD_ERROR_HANDLE = -12
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

        [DllImport("kernel32.dll", EntryPoint = "OpenFileMapping",
            SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr OpenFileMapping(enumFileMap dwDesiredAccess, bool bInheritHandle,
           string lpName);

        [DllImport("Kernel32.dll", EntryPoint = "MapViewOfFile",
            SetLastError = true, CharSet = CharSet.Unicode)]
        protected static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject,
          enumFileMap dwDesiredAccess, uint dwFileOffsetHigh,
          uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", EntryPoint = "UnmapViewOfFile",
            SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        protected static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeWaitHandle CreateEvent(IntPtr lpEventAttributes, bool bManualReset,
                                                 bool bInitialState, string lpName);

        private void ListenerThread()
        {
            IntPtr queuePtr;

            IntPtr map = CreateFileMapping(0xFFFFFFFFu, IntPtr.Zero,
                                           enumProtect.PAGE_READWRITE,
                                           0, (uint)Marshal.SizeOf(typeof(MessageQueue)),
                                           "BadgerPacketQueue");
            if (Marshal.GetLastWin32Error() == ERROR_ALREADY_EXISTS)
            {
                map = OpenFileMapping(enumFileMap.FILE_MAP_WRITE, false, "BadgerPacketQueue");
            }

            queuePtr = MapViewOfFile(map, enumFileMap.FILE_MAP_WRITE,
                                     0, 0, (uint)Marshal.SizeOf(typeof(MessageQueue)));

            Mutex queueMutex = new Mutex(false, "BadgerQueueMutex");

            AutoResetEvent readyEvent = new AutoResetEvent(false);
            readyEvent.SafeWaitHandle = CreateEvent(IntPtr.Zero,
                                                    false, // bManualReset
                                                    false, // bInitialState
                                                    "BadgerPacketReady");

            IntPtr numElementsPtr = (IntPtr)(queuePtr.ToInt64() +
                                             Marshal.OffsetOf(typeof(MessageQueue), "num_elements").ToInt64());
            IntPtr elementsPtr = (IntPtr)(queuePtr.ToInt64() +
                                          Marshal.OffsetOf(typeof(MessageQueue), "elements").ToInt64());

            int elementSize = Marshal.SizeOf(typeof(MessageQueueElement));
            IntPtr tmpElementPtr = Marshal.AllocHGlobal(elementSize);

            try
            {
                //
                // First off, upload softwall rules
                //
                if (!queueMutex.WaitOne(5000, false))
                {
                    Stopped();
                    return;
                }

                try
                {
                    // Empty message queue
                    //Marshal.WriteInt32(numElementsPtr, 0);

                    Marshal.WriteIntPtr(queuePtr, Marshal.OffsetOf(typeof(MessageQueue), "num_softwall_rules").ToInt32(),
                                        (IntPtr) rules.Length);

                    IntPtr p = (IntPtr)(queuePtr.ToInt64() +
                        Marshal.OffsetOf(typeof(MessageQueue), "softwall_rules").ToInt64());
                    foreach (SoftwallRule rule in rules)
                    {
                        Marshal.StructureToPtr(rule, p, false);

                        p = (IntPtr)(p.ToInt64() + Marshal.SizeOf(typeof(SoftwallRule)));
                    }
                }
                finally
                {
                    queueMutex.ReleaseMutex();
                }

                //
                // Then start monitoring
                //
                while (running)
                {
                    Int32 len;
                    byte[] bytes = null;

                    Thread.Sleep(1000);

                    if (readyEvent.WaitOne(500, false))
                    {
                        if (!queueMutex.WaitOne(5000, false))
                        {
                            Stopped();
                            return;
                        }

                        try
                        {
                            len = Marshal.ReadInt32(numElementsPtr);

                            if (len > 0)
                            {
                                bytes = new byte[len * Marshal.SizeOf(typeof(MessageQueueElement))];

                                Marshal.Copy(elementsPtr, bytes, 0, bytes.Length);

                                Marshal.WriteInt32(numElementsPtr, 0);
                            }
                        }
                        finally
                        {
                            queueMutex.ReleaseMutex();
                        }

                        if (bytes != null)
                        {
                            MessageQueueElement[] elements = new MessageQueueElement[len];

                            for (int i = 0; i < len; i++)
                            {
                                Marshal.Copy(bytes, i * elementSize, tmpElementPtr, elementSize);
                                elements[i] = (MessageQueueElement)Marshal.PtrToStructure(tmpElementPtr, typeof(MessageQueueElement));
                            }

                            MessageElementsReceived(elements);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tmpElementPtr);
            }

            Stopped();
        }
    }

    public enum MessageType
    {
        MESSAGE_TYPE_MESSAGE = 0,
        MESSAGE_TYPE_PACKET = 1,
    };

    public enum MessageContext
    {
        MESSAGE_CTX_INFO = 0,
        MESSAGE_CTX_WARNING = 1,
        MESSAGE_CTX_ERROR = 2,
        MESSAGE_CTX_SOCKET_LISTENING = 3,
        MESSAGE_CTX_SOCKET_CONNECTING = 4,
        MESSAGE_CTX_SOCKET_CONNECTED = 5,
        MESSAGE_CTX_SOCKET_DISCONNECTED = 6,
        MESSAGE_CTX_SOCKET_RESET = 7,
        MESSAGE_CTX_ACTIVESYNC_DEVICE = 8,
        MESSAGE_CTX_ACTIVESYNC_STATUS = 9,
        MESSAGE_CTX_ACTIVESYNC_SUBSTATUS = 10,
        MESSAGE_CTX_ACTIVESYNC_WZ_STATUS = 11,
    };

    public enum PacketDirection
    {
        PACKET_DIRECTION_INVALID = 0,
        PACKET_DIRECTION_INCOMING = 1,
        PACKET_DIRECTION_OUTGOING = 2,
    };
}
