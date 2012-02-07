using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace IronCow.Search
{
    public class TermNode : Node
    {
        public string Term { get; set; }

        public TermNode(string term)
        {
            Term = term;
        }

        public override bool ShouldInclude(SearchContext context)
        {
            return context.Task.Name.ToLower().Contains(Term.ToLower());
        }

        public override bool NeedsArchivedLists()
        {
            return false;
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            return new EmptyEnumerator<Node>();
        }
    }
}
