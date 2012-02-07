using System;
using System.Runtime.Serialization;

namespace IronCow
{
    [DataContract]
    public class IronCowException : Exception
    {
        public IronCowException()
        {
        }

        public IronCowException(string message)
            : base(message)
        {
        }

        public IronCowException(string message, Exception inner)
            : base(message, inner)
        {
        }
     }
}
