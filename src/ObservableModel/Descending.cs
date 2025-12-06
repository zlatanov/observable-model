using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ObservableModel
{
    /// <summary>
    /// A simple wrapper that inverts the default comparison result
    /// of the underlying value which is assumed to be in ascending order.
    /// </summary>
    [DebuggerDisplay( "{Value, nq}" )]
    public readonly struct Descending<T> : IComparable<Descending<T>>
    {
        public Descending( T value )
        {
            Value = value;
        }


        public T Value { get; }


        public int CompareTo( Descending<T> other )
        {
            if ( Value is object && other.Value is object )
                return -Comparer<T>.Default.Compare( Value, other.Value );

            if ( Value is null && other.Value is null )
                return 0;

            if ( Value is null )
                return 1;

            return -1;
        }
    }
}
