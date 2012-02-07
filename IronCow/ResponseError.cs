using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace IronCow
{
    [DataContract]
    public class ResponseError
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// <para>
        /// 96: Invalid signature
        ///     The passed signature was invalid.
        /// 97: Missing signature
        ///     The call required signing but no signature was sent.
        /// 100: Invalid API Key
        ///     The API key passed was not valid or has expired.
        /// 105: Service currently unavailable
        ///     The requested service is temporarily unavailable.
        /// 111: Format "xxx" not found
        ///     The requested response format was not found.
        /// 112: Method "xxx" not found
        ///     The requested method was not found.
        /// 114: Invalid SOAP envelope
        ///     The SOAP envelope sent in the request could not be parsed.
        /// 115: Invalid XML-RPC Method Call
        ///     The XML-RPC request document could not be parsed. 
        /// </para>
        /// </remarks>
        [XmlAttribute("code", Form = XmlSchemaForm.Unqualified)]
        public int Code;

        [XmlAttribute("msg", Form = XmlSchemaForm.Unqualified)]
        public string Message;
    }
}
