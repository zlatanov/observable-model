using System;

namespace ObservableModel
{
    internal sealed class WeakObserver<T> : IObserver<T>, IDisposable
    {
        public static IDisposable Connect( IObservable<T> source, IObserver<T> observer ) => new WeakObserver<T>( source, observer );


        private WeakObserver( IObservable<T> source, IObserver<T> observer )
        {
            m_observer = new WeakReference<IObserver<T>>( observer );
            m_subscription = source.Subscribe( this );
        }


        public void OnCompleted()
        {
            if ( m_observer.TryGetTarget( out var observer ) )
            {
                observer.OnCompleted();
            }

            Dispose();
        }


        public void OnError( Exception error )
        {
            if ( m_observer.TryGetTarget( out var observer ) )
            {
                observer.OnError( error );
            }

            Dispose();
        }


        public void OnNext( T value )
        {
            if ( m_observer.TryGetTarget( out var observer ) )
            {
                observer.OnNext( value );
            }
            else
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            m_subscription.Dispose();
        }

        private readonly WeakReference<IObserver<T>> m_observer;
        private readonly IDisposable m_subscription;
    }
}
