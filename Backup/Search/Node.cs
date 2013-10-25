using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public abstract class Node : IEnumerable<Node>
    {
        #region Parenting Members
        private GroupNode mParent;
        public GroupNode Parent
        {
            get { return mParent; }
            set
            {
                if (mParent != null)
                    mParent.Remove(this);
                if (value != null)
                    value.Add(this);
            }
        }

        internal void SetParentInternal(GroupNode parent)
        {
            mParent = parent;
        } 
        #endregion

        #region Search Execution Members
        public abstract bool ShouldInclude(SearchContext context);

        public abstract bool NeedsArchivedLists();
        #endregion

        #region IEnumerable<Node> Members

        public abstract IEnumerator<Node> GetEnumerator();

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
