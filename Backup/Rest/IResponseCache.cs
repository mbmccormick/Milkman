using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace IronCow.Rest
{
    public interface IResponseCache
    {
        bool IsEnabled { get; set; }
        TimeSpan Validity { get; set; }

        void CacheResponse(string url, string response);
        bool HasCachedResponse(string url);
        string GetCachedResponse(string url);
        
        void InvalidateResponse(string url);
        void Clear();
    }
}
