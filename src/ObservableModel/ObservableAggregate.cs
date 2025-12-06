using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using ObservableModel.Subjects;

namespace ObservableModel
{
    internal interface IObservableAggregate
    {
        void RaiseMaybeChanged( PropertyChangedEventArgs? e );
    }

    public abstract class ObservableAggregate<TAccumulate> : INotifyPropertyChanged, IObservable<TAccumulate>
    {
        /// <summary>
        /// An even that is raised when the value might have changed.
        /// </summary>
        /// <remarks>
        /// In contrast to regular property changed
        /// events where the event is risen only when the value has actually changed, we raise the event here
        /// any time we know the collection or a property in the collection's items has changed. 
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        public abstract TAccumulate Value { get; }

        protected void RaiseValueChanged()
        {
            PropertyChanged?.Invoke( this, SharedPropertyChangedEventArgs.Value );

            if ( m_subject is not null && m_subject.HasObservers )
            {
                var current = m_subject.Current;
                var next = Value;

                if ( !EqualityComparer<TAccumulate>.Default.Equals( current, next ) )
                {
                    m_subject.OnNext( next );
                }
            }
        }

        public IDisposable Subscribe( IObserver<TAccumulate> observer )
        {
            m_subject ??= new BehaviorSubject<TAccumulate>( Value );

            return m_subject.Subscribe( observer ); ;
        }

        private BehaviorSubject<TAccumulate>? m_subject;
    }

    [DebuggerDisplay( "{Value}" )]
    internal sealed class ObservableAggregate<T, TAccumulate> : ObservableAggregate<TAccumulate>, IObservableAggregate
    {
        public ObservableAggregate( ObservableList<T> collection, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, string? funcExpression )
        {
            if ( funcExpression is not null && !funcExpression.AsSpan().Contains( '.' ) )
            {
                // Not a valid func expression. Most likely a variable was used.
                funcExpression = null;
            }

            m_collection = collection;
            m_seed = seed;
            m_func = func;
            m_funcExpression = funcExpression;
            m_value = default!;
        }

        public override TAccumulate Value
        {
            get
            {
                if ( m_dirty )
                {
                    m_value = Accumulate();
                    m_dirty = false;
                }

                return m_value;
            }
        }

        void IObservableAggregate.RaiseMaybeChanged( PropertyChangedEventArgs? e )
        {
            if ( e?.PropertyName is not null && m_funcExpression is not null )
            {
                // Ignore change notifications from properties not found in the aggregate expression
                if ( !m_funcExpression.Contains( e.PropertyName, StringComparison.Ordinal ) )
                    return;
            }

            if ( !m_dirty )
            {
                m_dirty = true;
                RaiseValueChanged();
            }
        }

        private TAccumulate Accumulate()
        {
            var accumulate = m_seed;

            foreach ( var item in m_collection )
            {
                accumulate = m_func( accumulate, item );
            }

            return accumulate;
        }

        private TAccumulate m_value;
        private bool m_dirty = true;

        private readonly ObservableList<T> m_collection;
        private readonly TAccumulate m_seed;
        private readonly Func<TAccumulate, T, TAccumulate> m_func;
        private readonly string? m_funcExpression;
    }
}
