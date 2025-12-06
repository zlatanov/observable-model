using System;
using System.Threading;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Skip<T> : IObservable<T>
    {
        public Skip( IObservable<T> source, int count )
        {
            if ( count < 0 )
                throw new ArgumentOutOfRangeException( nameof( count ), "The count must not be negative." );

            m_source = source;
            m_count = count;
        }


        public IDisposable Subscribe( IObserver<T> observer ) => new Sink( m_source, m_count, observer );


        private readonly IObservable<T> m_source;
        private readonly int m_count;


        private sealed class Sink : ObserverSink<T, T>
        {
            public Sink( IObservable<T> source, int count, IObserver<T> observer )
                : base( observer )
            {
                m_count = count;

                Connect( source );
            }


            public override void OnNext( T value )
            {
                if ( m_count < 0 || Interlocked.Decrement( ref m_count ) < 0 )
                {
                    ForwardOnNext( value );
                }
            }


            private int m_count;
        }
    }
}
