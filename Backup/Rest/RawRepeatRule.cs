using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawRepeatRule
    {
        [XmlAttribute("every")]
        [DataMember]
        public int Every { get; set; }

        [XmlText]
        [DataMember]
        public string Rule { get; set; }
    }
}
