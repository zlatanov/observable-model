using System;
using System.Diagnostics;
using System.Threading;

namespace ObservableModel
{
    internal abstract class ObserverSink<TSource, TResult> : IDisposable, IObserver<TSource>
    {
        protected ObserverSink( IObserver<TResult> observer )
        {
            m_observer = observer;
        }


        protected void Connect( IObservable<TSource> source )
        {
            Debug.Assert( m_subscription is null );

            m_subscription = source.Subscribe( this );
        }


        public void Dispose()
        {
            if ( Interlocked.Exchange( ref m_observer, null ) is object )
            {
                m_subscription?.Dispose();
                m_subscription = null;
            }
        }


        public void OnCompleted() => m_observer?.OnCompleted();


        public void OnError( Exception error ) => m_observer?.OnError( error );


        public abstract void OnNext( TSource value );


        protected void ForwardOnNext( TResult value ) => m_observer?.OnNext( value );


        private IObserver<TResult>? m_observer;
        private IDisposable? m_subscription;
    }
}
