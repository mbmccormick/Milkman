using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawAuthentication
    {
        [XmlElement("token")]
        [DataMember]
        public string Token { get; set; }

        [XmlElement("perms")]
        [DataMember]
        public AuthenticationPermissions Permissions { get; set; }

        [XmlElement("user")]
        [DataMember]
        public RawUser User { get; set; }
    }
}
