using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawSettings
    {
        [XmlElement("timezone")]
        [DataMember]
        public string TimeZone { get; set; }

        [XmlElement("dateformat")]
        [DataMember]
        public int DateFormat { get; set; }

        [XmlElement("timeformat")]
        [DataMember]
        public int TimeFormat { get; set; }

        [XmlElement("defaultlist")]
        [DataMember]
        public string DefaultList { get; set; }

        [XmlElement("language")]
        [DataMember]
        public string Language { get; set; }
    }
}
