using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawTask : RawRtmElement
    {
        [XmlAttribute("due")]
        [DataMember]
        public string Due { get; set; }

        [XmlAttribute("has_due_time")]
        [DataMember]
        public int HasDueTime { get; set; }

        [XmlAttribute("added")]
        [DataMember]
        public string Added { get; set; }

        [XmlAttribute("completed")]
        [DataMember]
        public string Completed { get; set; }

        [XmlAttribute("deleted")]
        [DataMember]
        public string Deleted { get; set; }

        // N, 1, 2, 3
        [XmlAttribute("priority")]
        [DataMember]
        public string Priority { get; set; }

        [XmlAttribute("postponed")]
        [DataMember]
        public string Postponed { get; set; }

        [XmlAttribute("estimate")]
        [DataMember]
        public string Estimate { get; set; }
    }
}
