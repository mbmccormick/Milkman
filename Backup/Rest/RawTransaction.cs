using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [DataContract]
    public class RawTransaction
    {
        [XmlAttribute("id")]
        [DataMember]
        public string IdString { get; set; }

        public int? Id
        {
            get
            {
                if (string.IsNullOrEmpty(IdString))
                    return null;
                return int.Parse(IdString);
            }
        }

        [XmlAttribute("undoable")]
        [DataMember]
        public int Undoable { get; set; }
    }
}
