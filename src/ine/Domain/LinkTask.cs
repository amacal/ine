using System;

namespace ine.Domain
{
    public class LinkTask
    {
        public Link[] Links { get; set; }

        public Action<Link, string> OnStatus;

        public Action<Link, Resource> OnCompleted;

        public Action<LogEntry> OnLog;
    }
}
