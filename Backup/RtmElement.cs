using System;
using IronCow.Rest;
using System.ComponentModel;

namespace IronCow
{
    public abstract class RtmElement
    {
        public const string UnsyncedId = "0";

        internal abstract Rtm Owner
        {
            get;
            set;
        }

        internal abstract bool Syncing
        {
            get;
            set;
        }

        public string Id { get; private set; }
        public bool IsSynced { get { return Id != UnsyncedId; } }

        protected RtmElement()
        {
            Id = UnsyncedId;
        }

        internal event EventHandler Synced;

        internal void Sync(RawRtmElement element)
        {
            if (!Syncing)
                throw new InvalidOperationException("This element currently has syncing disabled.");
            if (IsSynced && Id != element.Id)
                throw new InvalidOperationException(string.Format("This element was already synced. Has id '{0}', but RTM answered with id '{1}'.", Id, element.Id));

            if (!IsSynced)
            {
                Id = element.Id;
                DoSync(true, element);
            }
            else
            {
                DoSync(false, element);
            }

            OnSynced(EventArgs.Empty);
        }

        internal void Unsync()
        {
            if (!Syncing)
                throw new InvalidOperationException("This element currently has syncing disabled.");
            Id = UnsyncedId;
        }

        protected virtual void DoSync(bool firstSync, RawRtmElement element)
        {
        }

        protected virtual void OnSynced(EventArgs e)
        {
            if (Synced != null)
                Synced(this, e);
        }

        protected virtual void OnSyncingChanged()
        {
        }

        protected virtual void OnOwnerChanged()
        {
        }

        /// <summary>
        ///     This method is commonly used by other classes, providing one implementation 
        ///     here to eliminate duplicate code.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyChanged">By storing the event handler in this variable, we eliminate a race condition
        ///     where the original event handler becomes null and we later try to dereference it.</param>
        public static void OnPropertyChanged(object sender, string propertyName, PropertyChangedEventHandler propertyChanged)
        {
            if (propertyChanged != null)
                if (Rtm.Dispatcher != null && !Rtm.Dispatcher.CheckAccess())
                    Rtm.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            propertyChanged(sender, new PropertyChangedEventArgs(propertyName));
                        }));
                else
                    propertyChanged(sender, new PropertyChangedEventArgs(propertyName));
        }
    }
}
