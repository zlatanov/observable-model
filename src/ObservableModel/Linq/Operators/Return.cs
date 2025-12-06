using System;

namespace ObservableModel.Linq.Operators
{
    internal sealed class Return<T> : IObservable<T>
    {
        public Return( T value )
        {
            m_value = value;
        }



        public IDisposable Subscribe( IObserver<T> observer )
        {
            observer.OnNext( m_value );
            observer.OnCompleted();

            return Disposable.Empty;
        }


        private readonly T m_value;
    }
}
