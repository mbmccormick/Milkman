using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawTimezone
    {
        [XmlAttribute("id")]
        [DataMember]
        public int Id { get; set; }

        [XmlAttribute("name")]
        [DataMember]
        public string Name { get; set; }

        [XmlAttribute("dst")]
        [DataMember]
        public int Dst { get; set; }

        [XmlAttribute("offset")]
        [DataMember]
        public int Offset { get; set; }

        [XmlAttribute("current_offset")]
        [DataMember]
        public int CurrentOffset { get; set; }
    }
}
