using System;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Select<TSource, TResult> : IObservable<TResult>
    {
        public Select( IObservable<TSource> source, Func<TSource, TResult> selector )
        {
            m_source = source ?? throw new ArgumentNullException( nameof( source ) );
            m_selector = selector ?? throw new ArgumentNullException( nameof( selector ) );
        }


        public IDisposable Subscribe( IObserver<TResult> observer )
            => new Sink( m_source, m_selector, observer ?? throw new ArgumentNullException( nameof( observer ) ) );


        private readonly IObservable<TSource> m_source;
        private readonly Func<TSource, TResult> m_selector;


        private sealed class Sink : ObserverSink<TSource, TResult>
        {
            public Sink( IObservable<TSource> source, Func<TSource, TResult> selector, IObserver<TResult> observer )
                : base( observer )
            {
                m_selector = selector;

                Connect( source );
            }


            public override void OnNext( TSource value ) => ForwardOnNext( m_selector( value ) );


            private readonly Func<TSource, TResult> m_selector;
        }
    }
}
