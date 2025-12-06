using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ObservableModel.Linq.Operators;
using System.Threading.Tasks;
using System.Threading;
using ObservableModel.Linq;

namespace ObservableModel
{
    public static class ObservableExtensions
    {
        public static ObservableProperty<T> ToProperty<T>( this IObservable<T> source )
            => new( source );


        public static ObservableProperty<T> ToProperty<T>( this IObservable<T> source, T initialValue )
            => new( source, initialValue );


        /// <summary>
        /// An observable which provides values when any of the source properties used in the provided expression has changed.
        /// </summary>
        public static IObservable<TValue> Observe<T, TValue>( this T source, Expression<Func<T, TValue>> expression ) where T : INotifyPropertyChanged
            => new ObservableExpression<T, TValue>( source, expression );


        public static bool RaiseWhenChanged<TSource, TValue>( this PropertyChangedEventHandler? handler, TSource source, ref TValue oldValue, TValue newValue, [CallerMemberName] string? propertyName = null )
            where TSource : INotifyPropertyChanged
        {
            if ( !EqualityComparer<TValue>.Default.Equals( oldValue, newValue ) )
            {
                oldValue = newValue;
                Raise( handler, source, propertyName );

                return true;
            }

            return false;
        }


        public static void Raise<TSource>( this PropertyChangedEventHandler? handler, TSource source, [CallerMemberName] string? propertyName = null )
            where TSource : INotifyPropertyChanged
        {
            if ( handler is not null )
            {
                propertyName ??= String.Empty;
                var propertyChangedArgs = new PropertyChangedEventArgs( propertyName );
                handler( source, propertyChangedArgs );

                foreach ( var reference in propertyChangedArgs.GetRelatedEventArgs( source ) )
                {
                    Raise( handler, source, reference );
                }
            }
        }


        public static void Raise<TSource>( this PropertyChangedEventHandler? handler, TSource source, PropertyChangedEventArgs args )
            where TSource : INotifyPropertyChanged
        {
            if ( handler is not null )
            {
                handler( source, args );

                foreach ( var reference in args.GetRelatedEventArgs( source.GetType() ) )
                {
                    Raise( handler, source, reference );
                }
            }
        }

        public static IDisposable Subscribe<TSource>( this IObservable<TSource> source, Action<TSource> next, Action? completed = null, Action<Exception>? error = null )
           => new AnonymousObserver<TSource>( source, next, completed, error );


        public static IDisposable SubscribeWeak<TSource>( this IObservable<TSource> source, IObserver<TSource> observer )
            => WeakObserver<TSource>.Connect( source, observer );


        public static IObservable<TResult> Select<TSource, TResult>( this IObservable<TSource> source, Func<TSource, TResult> selector )
            => new Select<TSource, TResult>( source, selector );


        public static IObservable<TSource> Where<TSource>( this IObservable<TSource> source, Func<TSource, bool> predicate )
            => new Where<TSource>( source, predicate );


        public static IObservable<TSource> DistinctUntilChanged<TSource>( this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null )
            => new DistinctUntilChanged<TSource, TSource>( source, Identity<TSource>.Function, comparer );


        public static IObservable<TSource> DistinctUntilChanged<TSource, TKey>( this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null )
           => new DistinctUntilChanged<TSource, TKey>( source, keySelector, keyComparer );


        public static IObservable<TSource> Take<TSource>( this IObservable<TSource> source, int count )
            => new Take<TSource>( source, count );


        public static IObservable<TSource> Skip<TSource>( this IObservable<TSource> source, int count )
            => new Skip<TSource>( source, count );


        public static IObservable<TSource> ObserveOn<TSource>( this IObservable<TSource> source, Action<Action> dispatcher )
            => new Dispatcher<TSource>( source, dispatcher );


        public static Task<TSource> FirstAsync<TSource>( this IObservable<TSource> source, CancellationToken cancellationToken = default )
            => new First<TSource>( source, cancellationToken ).Task;


        public static IAsyncEnumerable<TSource> AsEnumerable<TSource>( this IObservable<TSource> source )
            => new AsyncEnumerable<TSource>( source );


        public static IObservable<NewItems<T>> ObserveNewItems<T>( this IObservableEnumerable<T> items ) where T : notnull
            => new CollectionNewItems<T>( items );


        /// <summary>
        /// Turns the value in a form which when sorted will result in descending order.
        /// </summary>
        public static Descending<T> Descending<T>( this T value ) where T : IComparable<T>
            => new( value );


        /// <summary>
        /// Turns the value in a form which when sorted will result in descending order.
        /// </summary>
        public static Descending<T?> Descending<T>( this T? value ) where T : struct, IComparable<T>
            => new( value );
    }
}
