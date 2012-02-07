using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public class ParenthesisGroupNode : GroupNode, IList<Node>
    {
        private IList<Node> mNodes;

        public ParenthesisGroupNode()
        {
            mNodes = new List<Node>();
        }

        public override bool ShouldInclude(SearchContext context)
        {
            foreach (var node in this)
            {
                if (node == null)
                    throw new Exception();
                if (!node.ShouldInclude(context))
                    return false;
            }
            return true;
        }

        public override bool NeedsArchivedLists()
        {
            foreach (var node in this)
            {
                if (node == null)
                    throw new Exception();
                if (node.NeedsArchivedLists())
                    return true;
            }
            return false;
        }

        #region IList<Node> Members

        public int IndexOf(Node item)
        {
            return mNodes.IndexOf(item);
        }

        public void Insert(int index, Node item)
        {
            mNodes.Insert(index, item);
            if (item != null)
                item.SetParentInternal(this);
        }

        public void RemoveAt(int index)
        {
            if (this[index] != null)
                this[index].SetParentInternal(null);
            mNodes.RemoveAt(index);
        }

        public override Node this[int index]
        {
            get
            {
                return mNodes[index];
            }
            set
            {
                if (mNodes[index] != null)
                    mNodes[index].SetParentInternal(null);
                mNodes[index] = value;
                if (value != null)
                    value.SetParentInternal(this);
            }
        }

        #endregion

        #region ICollection<Node> Members

        public override void Add(Node item)
        {
            if (item != null && item.Parent != null)
                item.Parent.Remove(item);
            mNodes.Add(item);
            if (item != null)
                item.SetParentInternal(this);
        }

        public void Clear()
        {
            foreach (var node in this)
            {
                if (node != null)
                    node.SetParentInternal(null);
            }
            mNodes.Clear();
        }

        public bool Contains(Node item)
        {
            return mNodes.Contains(item);
        }

        public void CopyTo(Node[] array, int arrayIndex)
        {
            mNodes.CopyTo(array, arrayIndex);
        }

        public override int Count
        {
            get { return mNodes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public override bool Remove(Node item)
        {
            if (mNodes.Remove(item))
            {
                if (item != null)
                    item.SetParentInternal(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable<Node> Members

        public override IEnumerator<Node> GetEnumerator()
        {
            return mNodes.GetEnumerator();
        }

        #endregion
    }
}
