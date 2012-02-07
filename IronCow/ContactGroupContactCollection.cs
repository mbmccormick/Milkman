using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using IronCow.Rest;

namespace IronCow
{
    public class ContactGroupContactCollection : ICollection<Contact>, INotifyCollectionChanged
    {
        #region Private Members
        private ContactGroup mContactGroup;
        private List<Contact> mImpl;
        private object mSyncRoot; 
        #endregion

        #region Internal Properties
        internal bool Syncing { get; set; }
        internal object SyncRoot { get { return mSyncRoot; } } 
        #endregion

        #region Public Properties
        public Contact this[int index]
        {
            get
            {
                lock (mSyncRoot)
                {
                    return mImpl[index];
                }
            }
        }
        #endregion

        #region Construction
        internal ContactGroupContactCollection(ContactGroup contactGroup)
        {
            mContactGroup = contactGroup;
            mImpl = new List<Contact>();
            mSyncRoot = new object();

            if (contactGroup.Owner != null)
                Syncing = contactGroup.Owner.Syncing;
            else
                Syncing = true;
        } 
        #endregion

        #region Syncing
        internal void Sync(IEnumerable<RawContact> contacts)
        {
            if (!Syncing)
                throw new InvalidOperationException("Syncing is currently disabled on this contact group collection.");
            if (mContactGroup.Owner == null)
                throw new IronCowException("This contact group collection is not yet attached to an RTM instance.");

            Syncing = false;
            Clear();
            foreach (var contact in contacts)
            {
                Contact syncedContact = mContactGroup.Owner.Contacts.GetById(contact.Id, true);
                Add(syncedContact);
            }
            Syncing = true;
        }
        #endregion

        #region Requests
        private void AddContact(Contact item)
        {
            RestRequest request = new RestRequest("rtm.groups.addContact");
            request.Parameters.Add("timeline", mContactGroup.Owner.GetTimeline().ToString());
            request.Parameters.Add("group_id", mContactGroup.Id.ToString());
            request.Parameters.Add("contact_id", item.Id.ToString());
            mContactGroup.Owner.ExecuteRequest(request);
        }

        private void RemoveContact(Contact item)
        {
            RestRequest request = new RestRequest("rtm.groups.removeContact");
            request.Parameters.Add("timeline", mContactGroup.Owner.GetTimeline().ToString());
            request.Parameters.Add("group_id", mContactGroup.Id.ToString());
            request.Parameters.Add("contact_id", item.Id.ToString());
            mContactGroup.Owner.ExecuteRequest(request);
        } 
        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        #endregion

        #region ICollection<Contact> Members

        public void Add(Contact item)
        {
            lock (mSyncRoot)
            {
                if (mContactGroup.Owner == null)
                    throw new InvalidOperationException("Can't add contacts to a contact group until it has been added to an RTM instance.");

                if (!mContactGroup.Owner.Contacts.Contains(item))
                    mContactGroup.Owner.Contacts.Add(item);

                if (Syncing && mContactGroup.IsSynced)
                {
                    AddContact(item);
                }
                mImpl.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, mImpl.IndexOf(item)));
            }
        }

        public void Clear()
        {
            lock (mSyncRoot)
            {
                if (Syncing && mContactGroup.IsSynced)
                {
                    foreach (var item in this)
                    {
                        RemoveContact(item);
                    }
                }
                mImpl.Clear();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool Contains(Contact item)
        {
            lock (mSyncRoot)
            {
                return mImpl.Contains(item);
            }
        }

        public void CopyTo(Contact[] array, int arrayIndex)
        {
            lock (mSyncRoot)
            {
                mImpl.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (mSyncRoot)
                {
                    return mImpl.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Contact item)
        {
            lock (mSyncRoot)
            {
                if (Syncing && mContactGroup.IsSynced)
                {
                    RemoveContact(item);
                }
                int index = mImpl.IndexOf(item);
                bool result = mImpl.Remove(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                return result;
            }
        }

        #endregion

        #region IEnumerable<Contact> Members

        public IEnumerator<Contact> GetEnumerator()
        {
            lock (mSyncRoot)
            {
                return mImpl.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
