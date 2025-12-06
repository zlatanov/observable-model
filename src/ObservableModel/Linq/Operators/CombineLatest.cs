using System;
using System.Threading;

namespace ObservableModel.Linq.Operators
{
    internal sealed class CombineLatest<T1, T2, TResult> : IObservable<TResult>
    {
        public CombineLatest( IObservable<T1> observable1, IObservable<T2> observable2, Func<T1, T2, TResult> selector )
        {
            m_observable1 = observable1;
            m_observable2 = observable2;
            m_selector = selector;
        }

        public IDisposable Subscribe( IObserver<TResult> observer ) => new Sink( m_observable1, m_observable2, m_selector, observer );

        private readonly IObservable<T1> m_observable1;
        private readonly IObservable<T2> m_observable2;
        private readonly Func<T1, T2, TResult> m_selector;

        private sealed class Sink : IDisposable
        {
            public Sink( IObservable<T1> observable1,
                         IObservable<T2> observable2,
                         Func<T1, T2, TResult> selector,
                         IObserver<TResult> observer )
            {
                m_observer = observer;
                m_selector = selector;

                m_subscription1 = observable1.Subscribe( x =>
                {
                    lock ( m_lock )
                    {
                        m_hasLatest1 = true;
                        m_latest1 = x;

                        if ( m_hasLatest2 )
                        {
                            m_observer?.OnNext( m_selector( m_latest1, m_latest2 ) );
                        }
                    }
                }, OnCompleted, OnError );
                m_subscription2 = observable2.Subscribe( x =>
                {
                    lock ( m_lock )
                    {
                        m_hasLatest2 = true;
                        m_latest2 = x;

                        if ( m_hasLatest1 )
                        {
                            m_observer?.OnNext( m_selector( m_latest1, m_latest2 ) );
                        }
                    }
                }, OnCompleted, OnError );
            }

            public void Dispose()
            {
                lock ( m_lock )
                {
                    m_observer = null;
                    m_subscription1?.Dispose();
                    m_subscription1 = null;
                    m_subscription2?.Dispose();
                    m_subscription2 = null;
                }
            }

            private void OnCompleted()
            {
                Interlocked.Exchange( ref m_observer, null )?.OnCompleted();
                Dispose();
            }

            private void OnError( Exception exception )
            {
                Interlocked.Exchange( ref m_observer, null )?.OnError( exception );
                Dispose();
            }

            private readonly Lock m_lock = new();

            private IObserver<TResult>? m_observer;
            private IDisposable? m_subscription1;
            private IDisposable? m_subscription2;

            private readonly Func<T1, T2, TResult> m_selector;

            private bool m_hasLatest1;
            private T1 m_latest1 = default!;

            private bool m_hasLatest2;
            private T2 m_latest2 = default!;
        }
    }
}
