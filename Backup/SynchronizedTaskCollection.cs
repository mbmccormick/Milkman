using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IronCow
{
    public abstract class SynchronizedTaskCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged, ISyncing
    {
        #region Constants
        private const string CountName = "Count";
        private const string IndexerName = "Item[]";
        #endregion

        #region Private and Internal Members
        private object mSyncRoot;
        private List<T> mImpl;

        protected Task Task { get; private set; }
        protected IList<T> Items { get { return mImpl; } }
        protected object SyncRoot { get { return mSyncRoot; } }

        private bool mSyncing;
        internal bool Syncing
        {
            get { return mSyncing; }
            set
            {
                if (mSyncing != value)
                {
                    mSyncing = value;
                    OnSyncingChanged();
                }
            }
        }
        #endregion

        #region Construction
        protected SynchronizedTaskCollection(Task task)
        {
            mSyncRoot = new object();
            mImpl = new List<T>();

            Task = task;
            Syncing = task.Syncing;
        }
        #endregion

        #region Virtual Protected Members
        protected abstract Request CreateClearItemsRequest();
        protected abstract Request CreateAddItemRequest(T item);
        protected abstract Request CreateRemoveItemRequest(T item);

        protected virtual void OnSyncingChanged()
        {
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

        #region Unsyncing Changes
        internal void UnsyncedClear()
        {
            lock (mSyncRoot)
            {
                ClearItems();
                mImpl.Clear();
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        internal void UnsyncedAdd(T item)
        {
            lock (mSyncRoot)
            {
                AddItem(item);
                mImpl.Add(item);
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, mImpl.IndexOf(item)));
            }
        }

        internal void UnsyncedRemove(T item)
        {
            lock (mSyncRoot)
            {
                int index = mImpl.IndexOf(item);
                if (index >= 0)
                {
                    RemoveItem(item);
                    mImpl.RemoveAt(index);
                    OnPropertyChanged(CountName);
                    OnPropertyChanged(IndexerName);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                }
            }
        }
        #endregion

        #region ICollection<T> Members
        public void Add(T item)
        {
            lock (mSyncRoot)
            {
                if (Syncing && Task.IsSynced)
                {
                    Request request = CreateAddItemRequest(item);
                    Task.Owner.ExecuteRequest(request);
                }
                AddItem(item);
                mImpl.Add(item);
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, mImpl.IndexOf(item)));
            }
        }

        public void Clear()
        {
            lock (mSyncRoot)
            {
                if (Syncing && Task.IsSynced)
                {
                    Request request = CreateClearItemsRequest();
                    Task.Owner.ExecuteRequest(request);
                }
                ClearItems();
                mImpl.Clear();
                OnPropertyChanged(CountName);
                OnPropertyChanged(IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public bool Contains(T item)
        {
            lock (mSyncRoot)
            {
                return mImpl.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
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

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            lock (mSyncRoot)
            {
                int index = mImpl.IndexOf(item);
                if (index >= 0)
                {
                    if (Syncing && Task.IsSynced)
                    {
                        Request request = CreateRemoveItemRequest(item);
                        Task.Owner.ExecuteRequest(request);
                    }
                    RemoveItem(item);
                    mImpl.RemoveAt(index);
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

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            RtmElement.OnPropertyChanged(this, propertyName, PropertyChanged);
        }

        #endregion

        #region Protected Virtual Methods
        protected virtual void ClearItems()
        {
        }

        protected virtual void AddItem(T item)
        {
        }

        protected virtual void RemoveItem(T item)
        {
        }
        #endregion

        #region ISyncing Members

        bool ISyncing.Syncing
        {
            get { return Syncing; }
            set { Syncing = value; }
        }

        #endregion
    }
}
