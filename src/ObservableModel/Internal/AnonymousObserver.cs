using System;

namespace ObservableModel
{
    internal sealed class AnonymousObserver<T> : IDisposable, IObserver<T>
    {
        public AnonymousObserver( IObservable<T> source, Action<T> next, Action? completed, Action<Exception>? error )
        {
            m_next = next;
            m_completed = completed;
            m_error = error;
            m_subscription = source.Subscribe( this );
        }


        public void OnCompleted() => m_completed?.Invoke();


        public void OnError( Exception error ) => m_error?.Invoke( error );


        public void OnNext( T value ) => m_next?.Invoke( value );


        public void Dispose()
        {
            if ( m_subscription is object )
            {
                m_subscription.Dispose();
                m_subscription = null;

                m_next = null;
                m_completed = null;
                m_error = null;
            }
        }


        private Action<T>? m_next;
        private Action? m_completed;
        private Action<Exception>? m_error;
        private IDisposable? m_subscription;
    }
}
