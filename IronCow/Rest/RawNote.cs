using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawNote : RawRtmElement
    {
        [XmlAttribute("created")]
        [DataMember]
        public string Created { get; set; }

        [XmlAttribute("modified")]
        [DataMember]
        public string Modified { get; set; }

        [XmlAttribute("title")]
        [DataMember]
        public string Title { get; set; }

        [XmlText]
        [DataMember]
        public string Body { get; set; }
    }
}
