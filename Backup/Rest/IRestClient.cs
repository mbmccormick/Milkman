using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Net;

namespace IronCow.Rest
{
    public interface IRestClient
    {
        string ApiKey { get; }
        string SharedSecret { get; }
        string AuthToken { get; set; }

        bool UseHttps { get; set; }
        int Timeout { get; set; }
        TimeSpan Throttling { get; set; }

        IResponseCache Cache { get; set; }

        void GetResponse(string method, Dictionary<string, string> parameters, bool throwOnError, ResponseCallback callback);
    }

    public delegate void ResponseCallback(Response response);
}
