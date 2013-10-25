using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawLocation : RawRtmElement
    {
        [XmlAttribute("name")]
        [DataMember]
        public string Name { get; set; }

        [XmlAttribute("longitude")]
        [DataMember]
        public float Longitude { get; set; }

        [XmlAttribute("latitude")]
        [DataMember]
        public float Latitude { get; set; }

        [XmlAttribute("zoom")]
        [DataMember]
        public int Zoom { get; set; }

        [XmlAttribute("address")]
        [DataMember]
        public string Address { get; set; }

        [XmlAttribute("viewable")]
        [DataMember]
        public int Viewable { get; set; }
    }
}
