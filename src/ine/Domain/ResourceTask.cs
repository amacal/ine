using System;
using System.Threading;
using System.Threading.Tasks;

namespace ine.Domain
{
    public class ResourceTask
    {
        public Uri Url { get; set; }

        public string Hosting { get; set; }

        public string Destination { get; set; }

        public Scheduler Scheduler { get; set; }

        public CancellationToken Cancellation { get; set; }

        public Func<Captcha, Task<string>> OnCaptcha { get; set; }

        public Action<LogEntry> OnLog { get; set; }

        public Action<string> OnStatus { get; set; }

        public Action<long, long> OnProgress { get; set; }

        public Action<long> OnSpeed { get; set; }

        public Action<TimeSpan> OnEstimation { get; set; }

        public Action<bool> OnCompleted { get; set; }
    }
}
