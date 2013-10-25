using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Cache
{
    public class ResponseCacheEntry
    {
        public string Url { get; set; }
        public string Response { get; set; }
        public DateTime CreationTime { get; set; }
    }
}
