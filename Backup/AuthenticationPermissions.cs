using System.Xml.Serialization;

namespace IronCow
{
    public enum AuthenticationPermissions
    {
        [XmlEnum("read")]
        Read,
        [XmlEnum("write")]
        Write,
        [XmlEnum("delete")]
        Delete
    }
}
