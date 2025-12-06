using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ObservableModel
{
    public interface IObservableList : IList, IObservableEnumerable, IObservableObject
    {
    }

    public interface IObservableEnumerable : IEnumerable, INotifyCollectionChanged
    {
        /// <summary>
        /// Returns whether the current list supports <seealso cref="ItemsChanges" />. If the underlying item
        /// doesn't implement <seealso cref="System.ComponentModel.INotifyPropertyChanged" /> then this will return false.
        /// </summary>
        bool SupportsItemsChanges { get; }

        bool IsEmpty { get; }

        IObservable<ObservablePropertyChange> ItemsChanges { get; }

        IObservable<NotifyCollectionChangedEventArgs> CollectionChanges { get; }

        Type ItemType { get; }
    }

    public interface IObservableEnumerable<out T> : IEnumerable<T>, IObservableEnumerable
    {
    }
}
