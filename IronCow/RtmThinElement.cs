using System;

namespace IronCow
{
    public abstract class RtmThinElement : RtmElement
    {
        internal override Rtm Owner
        {
            get
            {
                var parent = ParentElement;
                if (parent == null)
                    return null;
                return ParentElement.Owner;
            }
            set
            {
                throw new InvalidOperationException("Can't set the owner of a thin Rtm element.");
            }
        }

        internal override bool Syncing
        {
            get
            {
                var parent = ParentElement;
                if (parent == null)
                    return false;
                return ParentElement.Syncing;
            }
            set
            {
                throw new InvalidOperationException("Can't set the syncing status of a thin Rtm element.");
            }
        }

        protected abstract RtmElement ParentElement
        {
            get;
        }
    }
}
