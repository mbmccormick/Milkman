using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawContact : RawRtmElement
    {
        [XmlAttribute("username")]
        [DataMember]
        public string UserName { get; set; }

        [XmlAttribute("fullname")]
        [DataMember]
        public string FullName { get; set; }
    }
}
