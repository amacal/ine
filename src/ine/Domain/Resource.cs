using System;

namespace ine.Domain
{
    public class Resource
    {
        public Uri Url { get; set; }

        public string Hosting { get; set; }

        public string Name { get; set; }

        public string Size { get; set; }

        public bool IsAvailable { get; set; }
    }
}
