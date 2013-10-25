using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace IronCow.Rest
{
    [XmlRoot("rsp", Namespace = "", IsNullable = false)]
    [DataContract]
    public class Response
    {
        [XmlAttribute("stat", Form = XmlSchemaForm.Unqualified)]
        [DefaultValue(ResponseStatus.Unknown)]
        [DataMember]
        public ResponseStatus Status = ResponseStatus.Unknown;

        [XmlElement("method", Form = XmlSchemaForm.Unqualified)]
        [DataMember]
        public string Method;

        [XmlElement("err", Form = XmlSchemaForm.Unqualified)]
        [DataMember]
        public ResponseError Error;

        [XmlElement("frob")]
        [DataMember]
        public string Frob;

        [XmlElement("auth")]
        [DataMember]
        public RawAuthentication Authentication;

        [XmlElement("transaction")]
        [DataMember]
        public RawTransaction Transaction;

        [XmlArray("contacts")]
        [XmlArrayItem("contact")]
        [DataMember]
        public RawContact[] Contacts;

        [XmlElement("contact")]
        [DataMember]
        public RawContact Contact;

        [XmlArray("groups")]
        [XmlArrayItem("group")]
        [DataMember]
        public RawGroup[] Groups;

        [XmlElement("group")]
        [DataMember]
        public RawGroup Group;

        [XmlArray("locations")]
        [XmlArrayItem("location")]
        [DataMember]
        public RawLocation[] Locations;

        [XmlArray("lists")]
        [XmlArrayItem("list")]
        [DataMember]
        public RawList[] Lists;

        [XmlElement("list")]
        [DataMember]
        public RawList List;

        [XmlArray("tasks")]
        [XmlArrayItem("list")]
        [DataMember]
        public RawList[] Tasks;

        [XmlElement("note")]
        [DataMember]
        public RawNote Note;

        [XmlElement("settings")]
        [DataMember]
        public RawSettings Settings;

        [XmlElement("user")]
        [DataMember]
        public RawUser User;

        [XmlElement("time")]
        [DataMember]
        public RawTime Time;

        [XmlElement("timeline")]
        [DataMember]
        public int Timeline;

        [XmlArray("timezones")]
        [XmlArrayItem("timezone")]
        [DataMember]
        public RawTimezone[] Timezones;
    }
}
