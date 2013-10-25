using System;
using System.Runtime.Serialization;

namespace IronCow
{
    [DataContract]
    public class RtmFatElement : RtmElement
    {
        [DataMember]
        private Rtm mOwner;
        internal override Rtm Owner
        {
            get { return mOwner; }
            set
            {
                if (mOwner == null && value != null)
                {
                    mOwner = value;
                    OnOwnerChanged();
                }
                else if (mOwner != null && value == null)
                {
                    mOwner = null;
                    OnOwnerChanged();
                }
                else if (mOwner != value)
                {
                    throw new InvalidOperationException("Can't change a valid RTM owner to another RTM instance.");
                }
            }
        }

        [DataMember]
        private bool mSyncing = true;
        internal override bool Syncing
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
    }
}
