using System;
using System.Collections.Generic;

namespace ObservableModel.Linq.Operators
{
    internal sealed class DistinctUntilChanged<TSource, TKey> : IObservable<TSource>
    {
        public DistinctUntilChanged( IObservable<TSource> source, Func<TSource, TKey> selector, IEqualityComparer<TKey>? comparer )
        {
            m_source = source;
            m_selector = selector;
            m_comparer = comparer ?? EqualityComparer<TKey>.Default;
        }


        public IDisposable Subscribe( IObserver<TSource> observer ) => new Sink( m_source, m_selector, m_comparer, observer );


        private readonly IObservable<TSource> m_source;
        private readonly Func<TSource, TKey> m_selector;
        private readonly IEqualityComparer<TKey> m_comparer;


        private sealed class Sink : ObserverSink<TSource, TSource>
        {
            public Sink( IObservable<TSource> source, Func<TSource, TKey> selector, IEqualityComparer<TKey> comparer, IObserver<TSource> observer )
                : base( observer )
            {
                m_selector = selector;
                m_comparer = comparer;

                m_key = default!;
                m_first = true;

                Connect( source );
            }


            public override void OnNext( TSource value )
            {
                var key = m_selector( value );

                if ( m_first )
                {
                    m_first = false;
                }
                else if ( m_comparer.Equals( m_key, key ) )
                {
                    return;
                }

                m_key = key;
                ForwardOnNext( value );
            }


            private readonly Func<TSource, TKey> m_selector;
            private readonly IEqualityComparer<TKey> m_comparer;

            private TKey m_key;
            private bool m_first;
        }
    }
}
