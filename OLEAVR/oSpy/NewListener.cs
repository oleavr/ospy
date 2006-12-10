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
using oSpy.IPC;
using System.Threading;

namespace oSpy
{
    public class AgentListener
    {
        public delegate void BlocksReceivedHandler(int newBlockCount, int newBlockSize);
        public event BlocksReceivedHandler BlocksReceived;

        public delegate void StoppedHandler();
        public event StoppedHandler Stopped;

        private List<byte[]> capturedBlocks;

        protected Server srv;
        private bool running;

        public AgentListener()
        {
            srv = new Server("messages");

            running = false;

            Stopped += new StoppedHandler(AgentListener_Stopped);
        }

        private void AgentListener_Stopped()
        {
            running = false;
        }

        public void Start()
        {
            if (running)
                throw new Exception("Already running");

            capturedBlocks = null;

            running = true;

            Thread thread = new Thread(ListenerThread);
            thread.Start();
        }

        public void Stop()
        {
            running = false;
        }

        public List<byte[]> GetCapturedBlocks()
        {
            return capturedBlocks;
        }

        private void ListenerThread()
        {
            List<byte[]> blocks = new List<byte[]>(100);
            int size = 0;

            while (running)
            {
                int prevCount = blocks.Count;

                int startTime = Environment.TickCount;

                do
                {
                    byte[] block = srv.ReadBlock(1000);
                    if (block != null)
                    {
                        blocks.Add(block);
                        size += block.Length;
                    }
                } while (Environment.TickCount - startTime < 1000 && running);

                if (blocks.Count > prevCount)
                {
                    BlocksReceived(blocks.Count, size);
                }
            }

            capturedBlocks = blocks;

            Stopped();
        }
    }
}
