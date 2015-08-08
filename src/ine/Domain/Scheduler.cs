using System.Collections.Generic;

namespace ine.Domain
{
    public class Scheduler
    {
        private readonly object synchronizer;
        private readonly HashSet<string> locks;

        public Scheduler()
        {
            this.synchronizer = new object();
            this.locks = new HashSet<string>();
        }

        public bool Schedule(string hosting)
        {
            lock (this.synchronizer)
            {
                return this.locks.Add(hosting);
            }
        }

        public void Release(string hosting)
        {
            lock (this.synchronizer)
            {
                this.locks.Remove(hosting);
            }
        }
    }
}
