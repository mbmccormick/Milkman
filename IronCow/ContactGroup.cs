using System;
using System.ComponentModel;
using IronCow.Rest;

namespace IronCow
{
    public class ContactGroup : RtmFatElement, INotifyPropertyChanged
    {
        public string Name { get; private set; }
        public ContactGroupContactCollection Contacts { get; private set; }

        public ContactGroup(string name)
        {
            Name = name;
            Contacts = new ContactGroupContactCollection(this);
        }

        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            // Save stuff we have locally.
            FirstSyncContactGroupData firstSyncData = null;
            if (firstSync)
                firstSyncData = new FirstSyncContactGroupData(this);

            // Sync the contact group properties and contents.
            RawGroup group = (RawGroup)element;
            Name = group.Name;
            Contacts.Sync(group.Contacts);            

            // Upload first sync stuff we had.
            if (firstSync)
                firstSyncData.Sync();

            // Notify of updates.
            OnPropertyChanged("Name");
        }

        protected override void OnSyncingChanged()
        {
            Contacts.Syncing = Syncing;
            base.OnSyncingChanged();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(this, propertyName, PropertyChanged);
        }

        #endregion
    }
}
