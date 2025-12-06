using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableModel.Linq.Operators
{
    internal sealed class First<TSource> : TaskCompletionSource<TSource>, IObserver<TSource>
    {
        public First( IObservable<TSource> observable, CancellationToken cancellationToken ) : base( TaskCreationOptions.RunContinuationsAsynchronously )
        {
            if ( cancellationToken.CanBeCanceled )
            {
                cancellationToken.Register( () =>
                {
                    if ( Dispose() )
                    {
                        TrySetCanceled( cancellationToken );
                    }
                } );
            }

            m_subscription = observable.Subscribe( this );
        }


        void IObserver<TSource>.OnCompleted()
        {
            if ( Dispose() )
            {
                SetException( new InvalidOperationException( "Sequence contains no matching element." ) );
            }
        }


        void IObserver<TSource>.OnError( Exception error )
        {
            if ( Dispose() )
            {
                SetException( error );
            }
        }


        void IObserver<TSource>.OnNext( TSource value )
        {
            if ( Dispose() )
            {
                SetResult( value );
            }
        }


        private bool Dispose()
        {
            var subscription = Interlocked.Exchange( ref m_subscription, null );

            if ( subscription is object )
            {
                subscription.Dispose();
                return true;
            }

            return false;
        }


        private IDisposable? m_subscription;
    }
}
