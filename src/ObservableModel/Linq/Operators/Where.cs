using System;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Where<T> : IObservable<T>
    {
        public Where( IObservable<T> source, Func<T, bool> predicate )
        {
            m_source = source;
            m_predicate = predicate;
        }


        public IDisposable Subscribe( IObserver<T> observer ) => new Sink( m_source, m_predicate, observer );


        private readonly IObservable<T> m_source;
        private readonly Func<T, bool> m_predicate;


        private sealed class Sink : ObserverSink<T, T>
        {
            public Sink( IObservable<T> source, Func<T, bool> predicate, IObserver<T> observer )
                : base( observer )
            {
                m_predicate = predicate;

                Connect( source );
            }


            public override void OnNext( T value )
            {
                if ( m_predicate( value ) )
                {
                    ForwardOnNext( value );
                }
            }


            private readonly Func<T, bool> m_predicate;
        }
    }
}
