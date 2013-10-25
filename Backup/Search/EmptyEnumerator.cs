using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    internal class EmptyEnumerator<T> : IEnumerator<T>
    {
        #region IEnumerator<Node> Members

        public T Current
        {
            get { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        #endregion
    }
}
