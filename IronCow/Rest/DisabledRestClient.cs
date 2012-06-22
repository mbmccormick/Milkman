using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace IronCow.Rest
{
    public class DisabledRestClient : IRestClient
    {
        private string mApiKey;
        private string mSharedSecret;

        private bool mAuthTokenDisabled;
        private string mAuthToken;

        private bool mUseHttps = false;
        private int mTimeout = 1000;
        private TimeSpan mThrottling = TimeSpan.Zero;

        private IResponseCache mCache;

        public DisabledRestClient()
            : this(true, true)
        {
        }

        public DisabledRestClient(bool disableApiKeyAndSharedSecret, bool disableAuthToken)
        {
            if (!disableApiKeyAndSharedSecret)
            {
                mApiKey = "Disabled.ApiKey";
                mSharedSecret = "Disabled.SharedSecret";
            }

            mAuthTokenDisabled = disableAuthToken;
        }

        #region IRestClient Members

        public string ApiKey
        {
            get
            {
                if (mApiKey == null)
                    throw new InvalidOperationException();
                return mApiKey;
            }
        }

        public string SharedSecret
        {
            get
            {
                if (mSharedSecret == null)
                    throw new InvalidOperationException();
                return mSharedSecret;
            }
        }

        public string AuthToken
        {
            get
            {
                if (mAuthTokenDisabled)
                    throw new InvalidOperationException();
                return mAuthToken;
            }
            set
            {
                if (mAuthTokenDisabled)
                    throw new InvalidOperationException();
                mAuthToken = value;
            }
        }

        public bool UseHttps
        {
            get
            {
                return mUseHttps;
            }
            set
            {
                mUseHttps = value;
            }
        }
        
        public int Timeout
        {
            get
            {
                return mTimeout;
            }
            set
            {
                mTimeout = value;
            }
        }

        public TimeSpan Throttling
        {
            get
            {
                return mThrottling;
            }
            set
            {
                mThrottling = value;
            }
        }

        public IResponseCache Cache
        {
            get
            {
                return mCache;
            }
            set
            {
                mCache = value;
            }
        }

        public void GetResponse(string method, Dictionary<string, string> parameters, bool throwOnError, ResponseCallback callback)
        {
            throw new InvalidOperationException();
        }

        #endregion
    }
}
