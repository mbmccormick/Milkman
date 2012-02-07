using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace IronCow.Search
{
    public class TokenCollection : Collection<Token>
    {
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (Token token in this)
            {
                builder.Append(token.ToString());
                builder.Append(" ");
            }
            return builder.ToString();
        }
    }
}
