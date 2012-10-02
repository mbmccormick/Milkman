using System;
using System.Net;

namespace IronCow.Resources
{
    public class StringsProvider
    {
        private readonly Strings _resources = new Strings();

        public Strings Resources
        {
            get { return _resources; }
        } 
    }
}
