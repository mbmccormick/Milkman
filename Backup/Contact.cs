using IronCow.Rest;

namespace IronCow
{
    public class Contact : RtmFatElement
    {
        public string UserName { get; private set; }
        public string FullName { get; private set; }

        public Contact(string usernameOrEmail)
        {
            UserName = usernameOrEmail;
        }

        internal Contact(RawContact contact)
        {
            Sync(contact);
        }

        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            RawContact contact = (RawContact)element;
            UserName = contact.UserName;
            FullName = contact.FullName;
        }
    }
}
