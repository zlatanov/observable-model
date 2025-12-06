using System;
using System.Collections.Immutable;
using System.Threading;

namespace ObservableModel.Subjects
{
    public sealed class BehaviorSubject<T> : IObservable<T>, IObserver<T>, IDisposable
    {
        public BehaviorSubject( T value )
        {
            Current = value;
        }

        public T Current { get; private set; }

        public bool HasObservers => m_observers.Length > 0;

        public void OnCompleted()
        {
            ImmutableArray<Observer> observers;

            lock ( m_lock )
            {
                ObjectDisposedException.ThrowIf( m_disposed, this );

                if ( m_completed )
                    return;

                observers = m_observers;

                m_completed = true;
                m_observers = [];
            }

            foreach ( var observer in observers )
            {
                observer.OnComleted();
            }
        }

        public void OnError( Exception error )
        {
            ImmutableArray<Observer> observers;

            lock ( m_lock )
            {
                ObjectDisposedException.ThrowIf( m_disposed, this );

                if ( m_completed )
                    return;

                observers = m_observers;

                m_completed = true;
                m_error = error;
                m_observers = [];
            }

            foreach ( var observer in observers )
            {
                observer.OnError( error );
            }
        }

        public void OnNext( T value )
        {
            ImmutableArray<Observer> observers;

            lock ( m_lock )
            {
                ObjectDisposedException.ThrowIf( m_disposed, this );

                if ( m_completed )
                    return;

                Current = value;
                observers = m_observers;
            }

            foreach ( var observer in observers )
            {
                observer.OnNext( value );
            }
        }

        public IDisposable Subscribe( IObserver<T> observer )
        {
            Observer? subscribed = null;

            lock ( m_lock )
            {
                ObjectDisposedException.ThrowIf( m_disposed, this );

                if ( !m_completed )
                    m_observers = m_observers.Add( subscribed = new Observer( this, observer ) );
            }

            if ( subscribed is null )
            {
                // The subject has been completed
                observer.OnNext( Current );

                if ( m_error is not null )
                {
                    observer.OnError( m_error );
                }
                else
                {
                    observer.OnCompleted();
                }

                return Disposable.Empty;
            }
            else
            {
                subscribed.OnNext( Current );
            }

            return subscribed;
        }

        public void Dispose()
        {
            ImmutableArray<Observer> observers;

            lock ( m_lock )
            {
                if ( m_disposed )
                    return;

                observers = m_observers;

                m_observers = [];
                m_disposed = true;
            }

            foreach ( var observer in observers )
            {
                observer.OnComleted();
            }
        }

        private readonly Lock m_lock = new();
        private ImmutableArray<Observer> m_observers = [];

        private bool m_disposed;
        private bool m_completed;
        private Exception? m_error;

        private sealed class Observer : IDisposable
        {
            public Observer( BehaviorSubject<T> source, IObserver<T> observer )
            {
                m_source = source;
                m_observer = observer;
            }

            internal void OnComleted() => m_observer?.OnCompleted();

            internal void OnError( Exception error ) => m_observer?.OnError( error );

            internal void OnNext( T value ) => m_observer?.OnNext( value );

            public void Dispose()
            {
                if ( Interlocked.Exchange( ref m_observer, null ) is object )
                {
                    lock ( m_source.m_lock )
                    {
                        m_source.m_observers = m_source.m_observers.Remove( this );
                    }
                }
            }

            private readonly BehaviorSubject<T> m_source;
            private IObserver<T>? m_observer;
        }
    }
}
