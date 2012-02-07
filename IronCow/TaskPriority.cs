using System.Xml.Serialization;

namespace IronCow
{
    public enum TaskPriority
    {
        [XmlEnum("N")]
        None = 0,
        [XmlEnum("1")]
        One = 1,
        [XmlEnum("2")]
        Two = 2,
        [XmlEnum("3")]
        Three = 3
    }
}
