using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public class BooleanNode : GroupNode
    {
        public BooleanType Type { get; set; }

        private Node mLeftChild;
        public Node LeftChild
        {
            get { return mLeftChild; }
            set { SetChild(value, ref mLeftChild); }
        }

        private Node mRightChild;
        public Node RightChild
        {
            get { return mRightChild; }
            set { SetChild(value, ref mRightChild); }
        }

        public override int Count
        {
            get { return 2; }
        }

        public override Node this[int i]
        {
            get
            {
                if (i == 0)
                    return LeftChild;
                if (i == 1)
                    return RightChild;
                throw new ArgumentOutOfRangeException();
            }
            set
            {
                if (i == 0)
                    LeftChild = value;
                else if (i == 1)
                    RightChild = value;
                else
                    throw new ArgumentOutOfRangeException();
            }
        }

        public BooleanNode(BooleanType type)
        {
            Type = type;
        }

        public override bool ShouldInclude(SearchContext context)
        {
            if (LeftChild == null || RightChild == null)
                throw new Exception();

            switch (Type)
            {
                case BooleanType.And:
                    return LeftChild.ShouldInclude(context) && RightChild.ShouldInclude(context);
                case BooleanType.Or:
                    return LeftChild.ShouldInclude(context) || RightChild.ShouldInclude(context);
                default:
                    throw new NotImplementedException();
            }
        }

        public override bool NeedsArchivedLists()
        {
            if (LeftChild == null || RightChild == null)
                throw new Exception();
            return LeftChild.NeedsArchivedLists() || RightChild.NeedsArchivedLists();
        }

        public override void Add(Node node)
        {
            if (LeftChild == null)
                LeftChild = node;
            else if (RightChild == null)
                RightChild = node;
            else
                throw new Exception();
        }

        public override bool Remove(Node node)
        {
            if (node == LeftChild)
                LeftChild = null;
            else if (node == RightChild)
                RightChild = null;
            else
                return false;
            return true;
        }

        private void SetChild(Node node, ref Node which)
        {
            // Remove existing child.
            System.Diagnostics.Debug.Assert(which == mLeftChild || which == mRightChild);
            if (which != null)
            {
                System.Diagnostics.Debug.Assert(which.Parent == this);
                which.SetParentInternal(null);
            }

            // Add new child.
            if (node != null && node.Parent != null)
                node.Parent.Remove(node);
            which = node;
            if (node != null)
                node.SetParentInternal(this);
        }

        #region Enumeration
        private class Enumerator : IEnumerator<Node>
        {
            private int m_index;
            private BooleanNode m_node;

            public Enumerator(BooleanNode node)
            {
                m_node = node;
                m_index = 0;
            }

            #region IEnumerator<Node> Members

            public Node Current
            {
                get
                {
                    if (m_index == 0)
                        return m_node.LeftChild;
                    if (m_index == 1)
                        return m_node.RightChild;
                    throw new Exception();
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (m_index == 0)
                {
                    m_index = 1;
                    return true;
                }
                else
                {
                    m_index = 2;
                    return false;
                }
            }

            public void Reset()
            {
                m_index = 0;
            }

            #endregion
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            return new Enumerator(this);
        }
        #endregion
    }
}
