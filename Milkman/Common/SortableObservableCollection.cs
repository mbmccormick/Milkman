using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Milkman.Common
{
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        // Use Comparer<T>.Default
        public void Sort()
        {
            Sort(null);
        }

        // Pass custom comparison
        public void Sort(Comparison<T> comparison)
        {
            (Items as List<T>).Sort(comparison);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
