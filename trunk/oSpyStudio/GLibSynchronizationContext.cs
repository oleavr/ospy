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
using System.Collections;
using System.Threading;

namespace oSpyStudio
{
    internal class PendingSendOrPost
    {
        private SendOrPostCallback cb;
        private System.Object state;
        private ManualResetEvent ev;
        
        public PendingSendOrPost(SendOrPostCallback cb, System.Object state)
        {
            this.cb = cb;
            this.state = state;
            this.ev = new ManualResetEvent(false);
        }
        
        public void Invoke()
        {
            cb(state);
            ev.Set();
        }
        
        public void Wait()
        {
            ev.WaitOne();
        }
    }

    public class GLibSynchronizationContext : SynchronizationContext
    {
        private Queue pendingEvents = new Queue();
        
        public override SynchronizationContext CreateCopy()
        {
            return new GLibSynchronizationContext();
        }
        
        public override void Post(SendOrPostCallback d, System.Object state)
        {
            lock (pendingEvents.SyncRoot)
            {
                pendingEvents.Enqueue(new PendingSendOrPost(d, state));
            }
            
            GLib.Idle.Add(ProcessEventsIdleHandler);
        }

        // TODO: Call directly if called from the same thread
        public override void Send(SendOrPostCallback d, System.Object state)
        {
            PendingSendOrPost ev = new PendingSendOrPost(d, state);

            lock (pendingEvents.SyncRoot)
            {
                pendingEvents.Enqueue(ev);
            }
            
            GLib.Idle.Add(ProcessEventsIdleHandler);
            
            ev.Wait();
        }

        private bool ProcessEventsIdleHandler()
        {
            while (true)
            {
                PendingSendOrPost ev = null;

                lock (pendingEvents.SyncRoot)
                {
                    if (pendingEvents.Count == 0)
                        return false;
                        
                    ev = pendingEvents.Dequeue() as PendingSendOrPost;
                }
                
                ev.Invoke();
            }
        }
    }
}
