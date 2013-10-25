using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawGroup : RawRtmElement
    {
        [XmlAttribute("name")]
        [DataMember]
        public string Name { get; set; }

        [XmlArray("contacts")]
        [XmlArrayItem("contact")]
        [DataMember]
        public RawContact[] Contacts { get; set; }
    }
}
