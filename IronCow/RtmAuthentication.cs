using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JeffWilcox.Utilities.Silverlight;

namespace IronCow
{
    public static class RtmAuthentication
    {
        public const string AuthenticationServiceUrl = "http://www.rememberthemilk.com/services/auth/";

        public static string GetAuthenticationUrl(string frob, string apiKey, string sharedSecret, AuthenticationPermissions permission)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("api_key", apiKey);
            parameters.Add("perms", permission.ToString().ToLowerInvariant());
            parameters.Add("frob", frob);

            string apiSig = GetApiSig(sharedSecret, parameters);

            parameters.Add("api_sig", apiSig);

            string[] keys = parameters.Keys.ToArray();
            Array.Sort(keys);

            StringBuilder urlBuilder = new StringBuilder(AuthenticationServiceUrl);
            foreach (string key in keys)
            {
                if (urlBuilder.Length == AuthenticationServiceUrl.Length)
                    urlBuilder.Append("?");
                else
                    urlBuilder.Append("&");

                urlBuilder.Append(key);
                urlBuilder.Append("=");
                urlBuilder.Append(parameters[key]);
            }

            return urlBuilder.ToString();
        }

        public static string GetApiSig(string sharedSecret, Dictionary<string, string> parameters)
        {
            string[] keys = parameters.Keys.ToArray();
            Array.Sort(keys);
            StringBuilder builder = new StringBuilder(parameters.Count * 10);
            builder.Append(sharedSecret);
            foreach (var key in keys)
            {
                builder.Append(key);
                builder.Append(parameters[key]);
            }

            MD5 md5 = MD5.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            byte[] hashedBytes = md5.ComputeHash(bytes);

            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
