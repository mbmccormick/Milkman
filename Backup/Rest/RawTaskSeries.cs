using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawTaskSeries : RawRtmElement
    {
        [XmlAttribute("created")]
        [DataMember]
        public string Created { get; set; }

        [XmlAttribute("modified")]
        [DataMember]
        public string Modified { get; set; }

        [XmlAttribute("name")]
        [DataMember]
        public string Name { get; set; }

        [XmlAttribute("source")]
        [DataMember]
        public string Source { get; set; }

        [XmlAttribute("url")]
        [DataMember]
        public string Url { get; set; }

        [XmlAttribute("location_id")]
        [DataMember]
        public string LocationId { get; set; }

        [XmlElement("rrule")]
        [DataMember]
        public RawRepeatRule RepeatRule { get; set; }

        [XmlArray("tags")]
        [XmlArrayItem("tag")]
        [DataMember]
        public string[] Tags { get; set; }

        //TODO: participants

        [XmlArray("notes")]
        [XmlArrayItem("note")]
        [DataMember]
        public RawNote[] Notes { get; set; }

        [XmlElement("task")]
        [DataMember]
        public RawTask[] Tasks { get; set; }
    }
}
