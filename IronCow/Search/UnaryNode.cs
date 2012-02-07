using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public enum UnaryType
    {
        Not
    }

    public class UnaryNode : GroupNode
    {
        public UnaryType Type { get; private set; }

        private Node mChild;
        public Node Child
        {
            get { return mChild; }
            set
            {
                // Remove existing child.
                if (mChild != null)
                    mChild.SetParentInternal(null);

                // Add new child.
                if (value != null && value.Parent != null)
                    value.Parent.Remove(value);
                mChild = value;
                if (mChild != null)
                    mChild.SetParentInternal(this);
            }
        }

        public override int Count
        {
            get { return 1; }
        }

        public override Node this[int i]
        {
            get
            {
                if (i == 0)
                    return Child;
                throw new ArgumentOutOfRangeException();
            }
            set
            {
                if (i == 0)
                    Child = value;
                throw new ArgumentOutOfRangeException();
            }
        }

        public UnaryNode(UnaryType type)
        {
            Type = type;
        }

        public override bool ShouldInclude(SearchContext context)
        {
            if (Child == null)
                throw new Exception();
            return !Child.ShouldInclude(context);
        }

        public override bool NeedsArchivedLists()
        {
            // Can't really have a "includeArchived" operator in a "NOT" statement
            // since by default they're not included.
            return false;
        }

        public override void Add(Node node)
        {
            if (Child == null)
                Child = node;
            else
                throw new NotSupportedException();
        }

        public override bool Remove(Node node)
        {
            if (Child == node)
            {
                Child = null;
                return true;
            }
            return false;
        }

        #region Enumeration
        private class Enumerator : IEnumerator<Node>
        {
            private bool mValidCurrent;
            private Node mNode;

            public Enumerator(Node node)
            {
                mNode = node;
                mValidCurrent = false;
            }

            #region IEnumerator<Node> Members

            public Node Current
            {
                get
                {
                    if (mValidCurrent)
                        return mNode;
                    return null;
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
                mValidCurrent = !mValidCurrent;
                return mValidCurrent;
            }

            public void Reset()
            {
                mValidCurrent = false;
            }

            #endregion
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            return new Enumerator(Child);
        } 
        #endregion
    }
}
