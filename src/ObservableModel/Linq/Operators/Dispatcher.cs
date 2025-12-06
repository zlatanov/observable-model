using System;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Dispatcher<TSource> : IObservable<TSource>
    {
        public Dispatcher( IObservable<TSource> source, Action<Action> dispatcher )
        {
            m_source = source;
            m_dispatcher = dispatcher;
        }


        public IDisposable Subscribe( IObserver<TSource> observer ) => new Sink( m_source, m_dispatcher, observer );


        private readonly IObservable<TSource> m_source;
        private readonly Action<Action> m_dispatcher;


        private sealed class Sink : IObserver<TSource>, IDisposable
        {
            public Sink( IObservable<TSource> source, Action<Action> dispatcher, IObserver<TSource> observer )
            {
                m_dispatcher = dispatcher;
                m_observer = observer;

                m_subscription = source.Subscribe( this );
            }


            void IObserver<TSource>.OnCompleted() => m_dispatcher( () => m_observer?.OnCompleted() );


            void IObserver<TSource>.OnError( Exception error ) => m_dispatcher( () => m_observer?.OnError( error ) );


            void IObserver<TSource>.OnNext( TSource value ) => m_dispatcher( () => m_observer?.OnNext( value ) );


            public void Dispose()
            {
                m_observer = null;
                m_subscription.Dispose();
            }


            private readonly Action<Action> m_dispatcher;
            private readonly IDisposable m_subscription;

            private IObserver<TSource>? m_observer;
        }
    }
}
