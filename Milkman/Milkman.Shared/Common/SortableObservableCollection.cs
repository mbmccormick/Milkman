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
            Sort(0, Count, null);
        }

        // Pass custom comparer
        public void Sort(IComparer<T> Comparer)
        {
            Sort(0, Count, null);
        }

        // Sort part of the collection
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            (Items as List<T>).Sort(index, count, comparer);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}