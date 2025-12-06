using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    public interface IReadOnlyObservableList<T> : IReadOnlyList<T>, INotifyPropertyChanged, IObservableEnumerable<T>
    {
        /// <summary>
        /// Returns the first item in the collection or default( T ).
        /// </summary>
        [MaybeNull]
        T First { get; }

        /// <summary>
        /// Returns the last item in the collection or default( T ).
        /// </summary>
        [MaybeNull]
        T Last { get; }

        Type IObservableEnumerable.ItemType => typeof( T );
    }
}
