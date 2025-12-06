using System;

namespace ObservableModel
{
    public static partial class ObservableList
    {
        public static IReadOnlyObservableList<T> Combine<T>( IObservableEnumerable<T> first, IObservableEnumerable<T> second )
            => new CombinedList<T>( first, second );


        public static IReadOnlyObservableList<TResult> Map<TSource, TResult>( this IObservableEnumerable<TSource> source, Func<TSource, TResult> factory )
            => new MappedList<TSource, TResult>( source, factory );
    }
}
