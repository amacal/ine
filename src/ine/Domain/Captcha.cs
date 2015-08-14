using System;
using System.Threading;
using System.Threading.Tasks;

namespace ine.Domain
{
    public class Captcha
    {
        public string Type { get; set; }

        public byte[] Data { get; set; }

        public Func<Task> Reload { get; set; }

        public Func<Task> ToAudio { get; set; }

        public Func<Task> ToImage { get; set; }

        public CancellationToken Cancellation { get; set; }
    }
}
