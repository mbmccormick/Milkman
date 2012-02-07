
namespace IronCow
{
    public delegate void SyncCallback();

    public abstract class SynchronizedRtmCollection<T> : RtmCollection<T>, ISyncing where T : RtmFatElement
    {
        private bool mSyncing;
        internal bool Syncing
        {
            get { return mSyncing; }
            set
            {
                mSyncing = value;
                OnSyncingChanged();
            }
        }

        protected SynchronizedRtmCollection(Rtm owner)
            : base(owner)
        {
            if (owner != null)
                Syncing = owner.Syncing;
            else
                Syncing = true;
        }

        public void Resync(SyncCallback callback)
        {
            if (Syncing)
            {
                using (new UnsyncedScope(this))
                {
                    DoResync(callback);
                }
            }
        }

        protected virtual void OnSyncingChanged()
        {
            foreach (var item in this)
            {
                item.Syncing = Syncing;
            }
        }

        protected override void ClearItems()
        {
            if (Syncing)
            {
                foreach (var item in this)
                {
                    if (item != null)
                    {
                        ExecuteRemoveElementRequest(item, () => { });
                    }
                }
            }
            base.ClearItems();
        }

        protected override void AddItem(T item)
        {
            if (item != null && Syncing)
                ExecuteAddElementRequest(item, () => { });
            base.AddItem(item);
        }

        protected override void RemoveItem(T item)
        {
            if (item != null && Syncing)
                ExecuteRemoveElementRequest(item, () => { });
            base.RemoveItem(item);
        }

        protected abstract void DoResync(SyncCallback callback);

        protected abstract void ExecuteAddElementRequest(T item, SyncCallback callback);

        protected abstract void ExecuteRemoveElementRequest(T item, SyncCallback callback);

        #region ISyncing Members

        bool ISyncing.Syncing
        {
            get { return Syncing; }
            set { Syncing = value; }
        }

        #endregion
    }
}
