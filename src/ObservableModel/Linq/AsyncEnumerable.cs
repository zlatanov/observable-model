using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ObservableModel.Linq
{
    public class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public AsyncEnumerable( IObservable<T> source )
        {
            m_source = source;
        }


        public IAsyncEnumerator<T> GetAsyncEnumerator( CancellationToken cancellationToken = default )
            => new Enumerator( m_source, cancellationToken );


        private readonly IObservable<T> m_source;


        private sealed class Enumerator : IAsyncEnumerator<T>, IObserver<T>
        {
            public Enumerator( IObservable<T> source, CancellationToken cancellationToken )
            {
                Current = default!;

                m_cancellationToken = cancellationToken;
                m_buffer = Channel.CreateUnbounded<T>( new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true,
                    AllowSynchronousContinuations = true
                } );
                m_subscription = source.Subscribe( this );
            }


            public T Current { get; private set; }


            public ValueTask DisposeAsync()
            {
                m_subscription.Dispose();
                return default;
            }


            public async ValueTask<bool> MoveNextAsync()
            {
                if ( await m_buffer.Reader.WaitToReadAsync( m_cancellationToken ).ConfigureAwait( false ) )
                {
                    if ( m_buffer.Reader.TryRead( out var next ) )
                    {
                        Current = next;
                        return true;
                    }
                }

                return false;
            }


            void IObserver<T>.OnCompleted() => m_buffer.Writer.TryComplete();


            void IObserver<T>.OnError( Exception error ) => m_buffer.Writer.TryComplete( error );


            void IObserver<T>.OnNext( T value ) => m_buffer.Writer.TryWrite( value );


            private readonly IDisposable m_subscription;
            private readonly Channel<T> m_buffer;
            private readonly CancellationToken m_cancellationToken;
        }
    }
}
