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
