using System.Xml.Serialization;

namespace IronCow
{
    public enum ResponseStatus
    {
        [XmlEnum("unknown")]
        Unknown,

        [XmlEnum("ok")]
        OK,

        [XmlEnum("fail")]
        Failed
    }
}
