using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IronCow.Rest;

namespace IronCow.Cache
{
    public class MemoryResponseCache : IResponseCache
    {
        private Dictionary<string, ResponseCacheEntry> mEntries = new Dictionary<string, ResponseCacheEntry>();

        #region Construction
        public MemoryResponseCache()
        {
            Validity = new TimeSpan(1, 0, 0);
        }
        #endregion

        #region IResponseCache Members

        public bool IsEnabled { get; set; }
        public TimeSpan Validity { get; set; }

        public void CacheResponse(string url, string response)
        {
            lock (this)
            {
                mEntries.Add(url, new ResponseCacheEntry() { Url = url, Response = response, CreationTime = DateTime.Now });
            }
        }

        public bool HasCachedResponse(string url)
        {
            lock (this)
            {
                ResponseCacheEntry entry;
                if (!mEntries.TryGetValue(url, out entry))
                    return false;
                return IsEntryValid(entry);
            }
        }

        public string GetCachedResponse(string url)
        {
            lock (this)
            {
                ResponseCacheEntry entry = mEntries[url];
                if (!IsEntryValid(entry))
                    throw new IronCowException("There is no valid entry in the cache for this URL.");
                return entry.Response;
            }
        }

        public void InvalidateResponse(string url)
        {
            lock (this)
            {
                mEntries.Remove(url);
            }
        }

        public void Clear()
        {
            lock (this)
            {
                mEntries.Clear();
            }
        }

        #endregion

        private bool IsEntryValid(ResponseCacheEntry entry)
        {
            TimeSpan sinceCreation = DateTime.Now.Subtract(entry.CreationTime);
            if (sinceCreation < Validity)
                return true;
            mEntries.Remove(entry.Url);
            return false;
        }
    }
}
