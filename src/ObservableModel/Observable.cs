using System;
using ObservableModel.Linq.Operators;

namespace ObservableModel
{
    public static class Observable
    {
        public static IObservable<T> Return<T>( T value ) => new Return<T>( value );


        public static IObservable<int> Interval( TimeSpan period ) => new Interval( period );


        public static IObservable<T> Create<T>( Func<IObserver<T>, IDisposable> subscribe ) => new Create<T>( subscribe );


        public static IObservable<TResult> CombineLatest<T1, T2, TResult>( IObservable<T1> observable1, IObservable<T2> observable2, Func<T1, T2, TResult> selector )
            => new CombineLatest<T1, T2, TResult>( observable1, observable2, selector );
    }
}
