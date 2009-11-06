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

        private bool enableEntryLimit = false;

        private uint levelTlsKey;

        #region Hook types

        private const string gdiDll = "gdi32.dll";
        private const string vcrDll = "msvcr90.dll";

        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern uint TlsAlloc();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr TlsGetValue(uint dwTlsIndex);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TlsSetValue(uint dwTlsIndex, IntPtr lpTlsValue);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate bool SwapBuffersHandler(IntPtr hdc);
        [DllImport(gdiDll, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SwapBuffers(IntPtr hdc);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate UIntPtr MallocHandler(int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr malloc(int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate UIntPtr CallocHandler(int num, int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr calloc(int num, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate UIntPtr ReallocHandler(UIntPtr memblock, int size);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern UIntPtr realloc(UIntPtr memblock, int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = false)]
        private delegate void FreeHandler(UIntPtr memblock);
        [DllImport(vcrDll, SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        private static extern void free(UIntPtr memblock);

        #endregion

        public Controller(RemoteHooking.IContext context,
                          string channelName,
                          SoftwallRule[] softwallRules)
        {
            string url = "ipc://" + channelName + "/" + channelName;
            manager = Activator.GetObject(typeof(IManager), url) as IManager;
            eventCoordinator = new EventCoordinator();

            levelTlsKey = TlsAlloc();
        }

        private void EnterAllocFunction()
        {
            AdjustAllocFunctionLevelBy(1);
        }

        private void LeaveAllocFunction()
        {
            AdjustAllocFunctionLevelBy(-1);
        }

        private void AdjustAllocFunctionLevelBy(int val)
        {
            IntPtr curVal = TlsGetValue(levelTlsKey);
            IntPtr newVal = new IntPtr(curVal.ToInt32() + val);
            TlsSetValue(levelTlsKey, newVal);
        }

        private int GetAllocFunctionLevel()
        {
            return TlsGetValue(levelTlsKey).ToInt32();
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

        private UIntPtr OnMalloc(int size)
        {
            EnterAllocFunction();
            UIntPtr result = malloc(size);
            LeaveAllocFunction();

            RegisterAllocation(size, result);
            return result;
        }

        private UIntPtr OnCalloc(int num, int size)
        {
            EnterAllocFunction();
            UIntPtr result = calloc(num, size);
            LeaveAllocFunction();

            RegisterAllocation(num * size, result);
            return result;
        }

        private UIntPtr OnRealloc(UIntPtr memblock, int size)
        {
            EnterAllocFunction();
            UIntPtr result = realloc(memblock, size);
            LeaveAllocFunction();

            RegisterReallocation(size, memblock, result);
            return result;
        }

        private void OnFree(UIntPtr memblock)
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

        private void RegisterAllocation(int size, UIntPtr address)
        {
            if (GetAllocFunctionLevel() > 0)
                return;

            lock (this)
            {
                if (tmpEvents == null)
                    return;
                var ev = new AllocateEvent(size, address);
                tmpEvents.Add(ev);
            }
        }

        private void RegisterReallocation(int size, UIntPtr oldAddress, UIntPtr newAddress)
        {
            if (GetAllocFunctionLevel() > 0)
                return;

            lock (this)
            {
                if (tmpEvents == null)
                    return;
                var ev = new ReallocateEvent(size, oldAddress, newAddress);
                tmpEvents.Add(ev);
            }
        }

        private void RegisterDeallocation(UIntPtr address)
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
            public UIntPtr Address;

            public AllocateEvent(int size, UIntPtr address)
            {
                Size = size;
                Address = address;
            }

            public override string ToString()
            {
                return String.Format("malloc({0}) => 0x{1:x8}", Size, Address.ToUInt32());
            }
        }
        
        private class ReallocateEvent : HeapEvent
        {
            public int Size;
            public UIntPtr OldAddress;
            public UIntPtr NewAddress;

            public ReallocateEvent(int size, UIntPtr oldAddress, UIntPtr newAddress)
            {
                Size = size;
                OldAddress = oldAddress;
                NewAddress = newAddress;
            }

            public override string ToString()
            {
                return String.Format("realloc(0x{0:x8}, {1}) => 0x{2:x8}", OldAddress.ToUInt32(), Size, NewAddress.ToUInt32());
            }
        }

        private class DeallocateEvent : HeapEvent
        {
            public UIntPtr Address;

            public DeallocateEvent(UIntPtr address)
            {
                Address = address;
            }

            public override string ToString()
            {
                return String.Format("free(0x{0:x8})", Address.ToUInt32());
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

            int allocCount = 0;
            int allocSize = 0;
            int deallocCount = 0;

            Dictionary<int, int> countForSize = new Dictionary<int, int>();
            foreach (HeapEvent ev in events)
            {
                // for now
                if (ev is DeallocateEvent)
                {
                    deallocCount++;
                    continue;
                }

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

                allocCount++;
                allocSize += size;
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendFormat("{0} allocations ({1} bytes), {2} deallocations\r\n",
                allocCount, allocSize, deallocCount);
            sb.AppendLine();

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
                sb.AppendLine(ev.ToString());

                count++;

                if (enableEntryLimit && count >= 100)
                    break;
            }

            if (enableEntryLimit)
            {
                int remaining = events.Count - count;
                if (remaining > 0)
                    sb.AppendFormat("...and {0} more...\r\n", remaining);
            }

            List<HeapEvent> leakedEvents = new List<HeapEvent>();
            foreach (HeapEvent ev in events)
            {
                UIntPtr removePtr = UIntPtr.Zero;

                if (ev is AllocateEvent)
                {
                    AllocateEvent allocEv = ev as AllocateEvent;
                    if (allocEv.Address != UIntPtr.Zero)
                        leakedEvents.Add(allocEv);
                }
                else if (ev is ReallocateEvent)
                {
                    ReallocateEvent reallocEv = ev as ReallocateEvent;
                    if (reallocEv.NewAddress != UIntPtr.Zero)
                    {
                        if (reallocEv.OldAddress != UIntPtr.Zero)
                        {
                            if (reallocEv.NewAddress == reallocEv.OldAddress)
                            {
                                // no change
                                // FIXME: in case we missed the original malloc we could get it here...
                            }
                            else
                            {
                                // relocated
                                leakedEvents.Add(reallocEv);
                                removePtr = reallocEv.OldAddress;
                            }
                        }
                        else
                        {
                            // new event
                            leakedEvents.Add(reallocEv);
                        }
                    }
                    else if (reallocEv.OldAddress != UIntPtr.Zero && reallocEv.Size == 0)
                    {
                        // free
                        removePtr = reallocEv.OldAddress;
                    }
                }
                else if (ev is DeallocateEvent)
                {
                    DeallocateEvent deallocEv = ev as DeallocateEvent;
                    removePtr = deallocEv.Address;
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (removePtr != UIntPtr.Zero)
                {
                    bool again = false;

                    do
                    {
                        again = false;

                        foreach (HeapEvent leakEv in leakedEvents)
                        {
                            UIntPtr address;

                            if (leakEv is AllocateEvent)
                                address = (leakEv as AllocateEvent).Address;
                            else if (leakEv is ReallocateEvent)
                                address = (leakEv as ReallocateEvent).NewAddress;
                            else
                                throw new NotImplementedException("Should not get here");

                            if (address == removePtr)
                            {
                                leakedEvents.Remove(leakEv);
                                again = true;
                                break;
                            }
                        }
                    }
                    while (again);
                }
            }

            if (leakedEvents.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Possible leaks:");
                sb.AppendLine("---------------");
                foreach (HeapEvent ev in leakedEvents)
                {
                    sb.AppendLine(ev.ToString());
                }
            }

            Event.InvocationOrigin origin = new Event.InvocationOrigin("HeapAgent", null, 42);
            PacketEvent pktEv = new PacketEvent(eventCoordinator, origin);
            pktEv.Data = Encoding.UTF8.GetBytes(sb.ToString());
            manager.Submit(new Event[] { pktEv });
        }
    }
}
