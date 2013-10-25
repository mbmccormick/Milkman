using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawList : RawRtmElement
    {
        [XmlAttribute("name")]
        [DataMember]
        public string Name { get; set; }

        [XmlElement("filter")]
        [DataMember]
        public string Filter { get; set; }

        [XmlAttribute("deleted")]
        [DataMember]
        public int Deleted { get; set; }

        [XmlAttribute("locked")]
        [DataMember]
        public int Locked { get; set; }

        [XmlAttribute("archived")]
        [DataMember]
        public int Archived { get; set; }

        [XmlAttribute("position")]
        [DataMember]
        public int Position { get; set; }

        [XmlAttribute("smart")]
        [DataMember]
        public int Smart { get; set; }

        [XmlElement("taskseries")]
        [DataMember]
        public RawTaskSeries[] TaskSeries { get; set; }

        [XmlElement("sort_order")]
        [DataMember]
        public int SortOrder { get; set; }

        [XmlArray("deleted")]
        [XmlArrayItem("taskseries")]
        [DataMember]
        public RawTaskSeries[] DeletedTaskSeries { get; set; }
    }
}
