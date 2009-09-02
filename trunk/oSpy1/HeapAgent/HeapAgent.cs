//
// Copyright (C) 2009  Ole André Vadla Ravnås <oleavr@gmail.com>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EasyHook;
using oSpy.Capture;
using System.Diagnostics;

namespace oHeapAgent
{
    public class Controller : IEntryPoint
    {
        private IManager manager;
        private EventCoordinator eventCoordinator;

        private LocalHook swapBuffersHook;
        private LocalHook mallocHook;
        private LocalHook callocHook;
        private LocalHook reallocHook;
        private LocalHook freeHook;

        #region Hook types

        private const string gdiDll = "gdi32.dll";
        private const string vcrDll = "msvcr90.dll";

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate bool SwapBuffersHandler(IntPtr hdc);
        [DllImport(gdiDll, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SwapBuffers(IntPtr hdc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate IntPtr MallocHandler(int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr malloc(int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate IntPtr CallocHandler(int num, int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr calloc(int num, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate IntPtr ReallocHandler(IntPtr memblock, int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr realloc(IntPtr memblock, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate void FreeHandler(IntPtr memblock);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free(IntPtr memblock);

        #endregion

        public Controller(RemoteHooking.IContext context,
                          string channelName,
                          SoftwallRule[] softwallRules)
        {
            string url = "ipc://" + channelName + "/" + channelName;
            manager = Activator.GetObject(typeof(IManager), url) as IManager;
            eventCoordinator = new EventCoordinator();
        }

        public void Run(RemoteHooking.IContext context,
                        string channelName,
                        SoftwallRule[] softwallRules)
        {
            try
            {
                swapBuffersHook = LocalHook.Create(
                    LocalHook.GetProcAddress(gdiDll, "SwapBuffers"),
                    new SwapBuffersHandler(OnSwapBuffers),
                    this);
                mallocHook = LocalHook.Create(
                    LocalHook.GetProcAddress(vcrDll, "malloc"),
                    new MallocHandler(OnMalloc),
                    this);
                callocHook = LocalHook.Create(
                    LocalHook.GetProcAddress(vcrDll, "calloc"),
                    new CallocHandler(OnCalloc),
                    this);
                reallocHook = LocalHook.Create(
                    LocalHook.GetProcAddress(vcrDll, "realloc"),
                    new ReallocHandler(OnRealloc),
                    this);
                freeHook = LocalHook.Create(
                    LocalHook.GetProcAddress(vcrDll, "free"),
                    new FreeHandler(OnFree),
                    this);

                Int32[] excludedThreads = new Int32[] { RemoteHooking.GetCurrentThreadId() };
                foreach (LocalHook hook in new LocalHook[] { swapBuffersHook, mallocHook, callocHook, reallocHook, freeHook })
                {
                    hook.ThreadACL.SetExclusiveACL(excludedThreads);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RemoteHooking.WakeUpProcess();

            int myPid = RemoteHooking.GetCurrentProcessId();

            try
            {
                manager.Ping(myPid);

                while (true)
                {
                    Thread.Sleep(500);
                    ProcessAllocations();
                    manager.Ping(myPid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("Terminating", "oHeapAgent", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool OnSwapBuffers(IntPtr hdc)
        {
            RegisterRenderFrame();
            return SwapBuffers(hdc);
        }

        private IntPtr OnMalloc(int size)
        {
            IntPtr result = malloc(size);
            RegisterAllocation(size, result);
            return result;
        }

        private IntPtr OnCalloc(int num, int size)
        {
            IntPtr result = calloc(num, size);
            RegisterAllocation(num * size, result);
            return result;
        }

        private IntPtr OnRealloc(IntPtr memblock, int size)
        {
            IntPtr result = realloc(memblock, size);
            RegisterReallocation(size, memblock, result);
            return result;
        }

        private void OnFree(IntPtr memblock)
        {
            RegisterDeallocation(memblock);
            free(memblock);
        }

        private List<HeapEvent> tmpEvents = null;
        private List<HeapEvent> lastFrameEvents = null;

        private void RegisterRenderFrame()
        {
            lock (this)
            {
                if (tmpEvents != null)
                    lastFrameEvents = tmpEvents;
                tmpEvents = new List<HeapEvent>();
            }
        }

        private void RegisterAllocation(int size, IntPtr address)
        {
            if (size == 1382528)
            {
                Console.WriteLine();
            }

            lock (this)
            {
                if (tmpEvents == null)
                    return;
                var ev = new AllocateEvent(size, address);
                tmpEvents.Add(ev);
            }
        }

        private void RegisterReallocation(int size, IntPtr oldAddress, IntPtr newAddress)
        {
            if (size == 1382528)
            {
                Console.WriteLine();
            } 
            
            lock (this)
            {
                if (tmpEvents == null)
                    return;
                var ev = new ReallocateEvent(size, oldAddress, newAddress);
                tmpEvents.Add(ev);
            }
        }

        private void RegisterDeallocation(IntPtr address)
        {
            lock (this)
            {
                if (tmpEvents == null)
                    return;
                var ev = new DeallocateEvent(address);
                tmpEvents.Add(ev);
            }
        }

        private class HeapEvent
        {
            protected HeapEvent()
            {
            }
        }

        private class AllocateEvent : HeapEvent
        {
            public int Size;
            public IntPtr Address;

            public AllocateEvent(int size, IntPtr address)
            {
                Size = size;
                Address = address;
            }
        }
        
        private class ReallocateEvent : HeapEvent
        {
            public int Size;
            public IntPtr OldAddress;
            public IntPtr NewAddress;

            public ReallocateEvent(int size, IntPtr oldAddress, IntPtr newAddress)
            {
                Size = size;
                OldAddress = oldAddress;
                NewAddress = newAddress;
            }
        }

        private class DeallocateEvent : HeapEvent
        {
            public IntPtr Address;

            public DeallocateEvent(IntPtr address)
            {
                Address = address;
            }
        }

        private void ProcessAllocations()
        {
            List<HeapEvent> events = null;

            lock (this)
            {
                if (lastFrameEvents == null)
                    return;
                events = lastFrameEvents;
                lastFrameEvents = null;
            }

            Dictionary<int, int> countForSize = new Dictionary<int, int>();
            foreach (HeapEvent ev in events)
            {
                // for now
                if (ev is DeallocateEvent)
                    continue;

                int size = 0;
                if (ev is AllocateEvent)
                {
                    AllocateEvent allocEv = ev as AllocateEvent;
                    size = allocEv.Size;
                }
                else if (ev is ReallocateEvent)
                {
                    ReallocateEvent reallocEv = ev as ReallocateEvent;
                    size = reallocEv.Size;
                }
                else
                    throw new NotImplementedException();

                int oldCount = 0;
                countForSize.TryGetValue(size, out oldCount);
                countForSize[size] = oldCount + 1;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("  Size  |  Count");
            sb.AppendLine("------------------");
            int[] sizes = countForSize.Keys.ToArray();
            Array.Sort(sizes);
            Array.Reverse(sizes);
            foreach (int size in sizes)
            {
                sb.AppendFormat("{0,7} | {1}\r\n", size, countForSize[size]);
            }

            sb.AppendLine();

            sb.AppendLine("Events:");
            sb.AppendLine("-------");
            int count = 0;
            foreach (HeapEvent ev in events)
            {
                if (ev is AllocateEvent)
                {
                    AllocateEvent allocEv = ev as AllocateEvent;
                    sb.AppendFormat("malloc({0}) => 0x{1:x8}\r\n", allocEv.Size, allocEv.Address);
                }
                else if (ev is ReallocateEvent)
                {
                    ReallocateEvent reallocEv = ev as ReallocateEvent;
                    sb.AppendFormat("realloc(0x{0:x8}, {1}) => 0x{2:x8}\r\n", reallocEv.OldAddress, reallocEv.Size, reallocEv.NewAddress);
                }
                else if (ev is DeallocateEvent)
                {
                    DeallocateEvent deallocEv = ev as DeallocateEvent;
                    sb.AppendFormat("free(0x{0:x8})\r\n", deallocEv.Address);
                }
                else
                {
                    throw new NotImplementedException();
                }

                count++;

                if (count >= 100)
                    break;
            }

            int remaining = events.Count - count;
            if (remaining > 0)
                sb.AppendFormat("...and {0} more...\r\n", remaining);

            Event.InvocationOrigin origin = new Event.InvocationOrigin("HeapAgent", null, 42);
            PacketEvent pktEv = new PacketEvent(eventCoordinator, origin);
            pktEv.Data = Encoding.UTF8.GetBytes(sb.ToString());
            manager.Submit(new Event[] { pktEv });
        }
    }
}
