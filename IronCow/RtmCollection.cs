using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace IronCow
{
    [DataContract]
    public class RtmCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged where T : RtmFatElement
    {
        #region Constants
        private const string CountName = "Count";
        private const string IndexerName = "Item[]";
        #endregion

        #region Internal & Protected Properties

        [DataMember]
        public Rtm Owner { get; set; }

        [DataMember]
        public List<T> Items { get; set; }

        [DataMember]
        public object SyncRoot { get; set; } 
        #endregion

        #region Public Properties
        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return Items[index];
                }
            }
        }
        #endregion

        #region Construction
        internal RtmCollection(Rtm owner)
        {
            Owner = owner;
            Items = new List<T>();
            SyncRoot = new object();
        } 
        #endregion

        #region Public Methods
        public T GetById(string id)
        {
            return GetById(id, false);
        }

        public T GetById(string id, bool throwIfNotFound)
        {
            lock (SyncRoot)
            {
                foreach (var item in this)
                {
                    if (item.Id == id)
                        return item;
                }
            }
            if (throwIfNotFound)
                throw new IronCowException(string.Format("No item with such id: '{0}'", id));
            return null;
        } 
        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                AddItem(item);
                Items.Add(item);
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Items.IndexOf(item)));
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                ClearItems();
                Items.Clear();
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return Items.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                Items.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return Items.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                int index = Items.IndexOf(item);
                if (index >= 0)
                {
                    RemoveItem(item);
                    Items.RemoveAt(index);
                    OnPropertyChanged(CountName);
                    OnPropertyChanged(IndexerName);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            lock (SyncRoot)
            {
                return Items.GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            RtmElement.OnPropertyChanged(this, propertyName, PropertyChanged);
        }

        #endregion

        #region ICollection Protected Virtual Methods

        protected virtual void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                {
                    item.Owner = null;
                }
            }
        }

        protected virtual void AddItem(T item)
        {
            if (item != null)
            {
                item.Owner = Owner;
                item.Syncing = Owner.Syncing;
            }
        }

        protected virtual void RemoveItem(T item)
        {
            if (item != null)
            {
                item.Owner = null;
            }
        }

        #endregion
    }
}
