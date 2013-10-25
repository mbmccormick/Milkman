using System;
using System.Runtime.Serialization;

namespace IronCow
{
    [DataContract]
    public class RtmException : Exception
    {
        public ResponseError ResponseError { get; private set; }

        public RtmException(ResponseError error)
            : base(error.Message)
        {
            ResponseError = error;
        }

        public RtmException(ResponseError error, string message)
            : base(message)
        {
            ResponseError = error;
        }

        public RtmException(ResponseError error, string message, Exception inner)
            : base(message, inner)
        {
            ResponseError = error;
        }

        
    }
}
