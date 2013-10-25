using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public abstract class GroupNode : Node
    {
        public abstract int Count { get; }
        public abstract Node this[int i] { get; set; }

        public abstract void Add(Node node);
        public abstract bool Remove(Node node);
    }
}
