using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace IronCow.Rest
{
    public class RawRtmElement
    {
        [XmlAttribute("id")]
        public string Id { get; set; }
    }
}
