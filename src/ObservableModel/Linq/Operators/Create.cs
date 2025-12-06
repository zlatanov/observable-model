using System;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Create<T> : IObservable<T>
    {
        public Create( Func<IObserver<T>, IDisposable> subscribe )
        {
            m_subscribe = subscribe;
        }


        public IDisposable Subscribe( IObserver<T> observer ) => m_subscribe( observer );


        private readonly Func<IObserver<T>, IDisposable> m_subscribe;
    }
}
