using System.Linq;

namespace IronCow
{
    internal class FirstSyncContactGroupData
    {
        private ContactGroup mContactGroup;

        public Contact[] Contacts;

        public FirstSyncContactGroupData(ContactGroup contactGroup)
        {
            mContactGroup = contactGroup;

            Contacts = contactGroup.Contacts.ToArray();
        }

        public void Sync()
        {
            foreach (var contact in Contacts)
            {
                mContactGroup.Contacts.Add(contact);
            }
        }
    }
}
