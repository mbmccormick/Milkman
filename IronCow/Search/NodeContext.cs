using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public class NodeContext
    {
        private Stack<GroupNode> mParentStack = new Stack<GroupNode>();
        
        public int ParentCount { get { return mParentStack.Count; } }
        public GroupNode LastParent { get { return mParentStack.Peek(); } }

        public NodeContext()
        {
        }

        public void PushParent(GroupNode parent)
        {
            if (mParentStack.Count > 0)
                mParentStack.Peek().Add(parent);
            mParentStack.Push(parent);
        }

        public void PopParent()
        {
            mParentStack.Pop();
        }

        public void AddChild(Node child)
        {
            if (mParentStack.Count == 0)
                throw new Exception();
            mParentStack.Peek().Add(child);
        }
    }
}
