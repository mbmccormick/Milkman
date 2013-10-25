using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawTime
    {
        [XmlAttribute("precision")]
        [DataMember]
        public string Precision { get; set; }

        [XmlText]
        [DataMember]
        public DateTime Time { get; set; }
    }
}
