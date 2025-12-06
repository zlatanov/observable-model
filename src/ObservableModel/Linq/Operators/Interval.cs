using System;
using System.Threading;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Interval : IObservable<int>
    {
        public Interval( TimeSpan period )
        {
            m_period = period;
        }


        public IDisposable Subscribe( IObserver<int> observer ) => new Sink( m_period, observer );


        private readonly TimeSpan m_period;


        private sealed class Sink : IDisposable
        {
            public Sink( TimeSpan period, IObserver<int> observer )
            {
                m_observer = observer;
                m_timer = new Timer( x => ( (Sink)x! ).OnTick(), this, period, period );
            }


            public void Dispose()
            {
                m_observer = null;
                m_timer.Dispose();
            }


            private void OnTick()
            {
                m_observer?.OnNext( m_index );
                m_index += 1;
            }


            private readonly Timer m_timer;
            private IObserver<int>? m_observer;

            private int m_index;
        }
    }
}
