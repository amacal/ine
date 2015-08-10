using System;
using System.Threading;
using System.Threading.Tasks;

namespace ine.Domain
{
    public class Captcha
    {
        public byte[] Data { get; set; }

        public Func<Task> Reload { get; set; }

        public CancellationToken Cancellation { get; set; }
    }
}
