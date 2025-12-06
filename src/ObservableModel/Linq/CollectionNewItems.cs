using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ObservableModel.Linq
{
    public readonly struct NewItems<T>
    {
        internal NewItems( ReadOnlyMemory<T> memory, bool isInitializing )
        {
            m_memory = memory;
            IsInitializing = isInitializing;
        }

        public ReadOnlySpan<T> Span => m_memory.Span;

        public bool IsInitializing { get; }

        public T[] ToArray() => m_memory.ToArray();

        private readonly ReadOnlyMemory<T> m_memory;
    }

    internal sealed class CollectionNewItems<T> : IObservable<NewItems<T>> where T : notnull
    {
        public CollectionNewItems( IObservableEnumerable<T> items )
        {
            m_items = items;
        }


        public IDisposable Subscribe( IObserver<NewItems<T>> observer ) => new Sink( m_items, observer );


        private readonly IObservableEnumerable<T> m_items;


        private sealed class Sink : ObserverSink<NotifyCollectionChangedEventArgs, NewItems<T>>
        {
            public Sink( IObservableEnumerable<T> items, IObserver<NewItems<T>> observer ) : base( observer )
            {
                var comparer = typeof( T ).IsClass ? (IEqualityComparer<T>)ReferenceEqualityComparer<T>.Default : EqualityComparer<T>.Default;

                m_snapshot = new HashSet<T>( items, comparer );
                m_items = items;

                Connect( items.CollectionChanges );
            }


            public override void OnNext( NotifyCollectionChangedEventArgs e )
            {
                switch ( e.Action )
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Remove:
                        OnAddOrReplace( e.OldItems, e.NewItems );
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        OnReset();
                        break;
                }
            }


            private void OnAddOrReplace( IList? oldItems, IList? newItems )
            {
                if ( oldItems?.Count > 0 )
                {
                    for ( var i = 0; i < oldItems.Count; ++i )
                    {
                        if ( oldItems[ i ] is T item )
                            m_snapshot.Remove( item );
                    }
                }

                if ( newItems?.Count > 0 )
                {
                    var array = new T[ newItems.Count ];
                    var arrayIndex = 0;

                    for ( var i = 0; i < newItems.Count; ++i )
                    {
                        if ( newItems[ i ] is T item && m_snapshot.Add( item ) )
                        {
                            array[ arrayIndex++ ] = item;
                        }
                    }

                    if ( arrayIndex > 0 )
                    {
                        ReadOnlyMemory<T> memory = new( array, start: 0, length: arrayIndex );
                        bool isInitializing = m_items is ITrackable t && t.IsInitializing;

                        ForwardOnNext( new NewItems<T>( memory, isInitializing ) );
                    }
                }
            }


            private void OnReset()
            {
                var newSnapshot = new HashSet<T>( m_items, m_snapshot.Comparer );

                if ( newSnapshot.Count > 0 )
                {
                    var array = new T[ newSnapshot.Count ];
                    var arrayIndex = 0;

                    foreach ( var item in newSnapshot )
                    {
                        if ( !m_snapshot.Contains( item ) )
                        {
                            array[ arrayIndex++ ] = item;
                        }
                    }

                    m_snapshot = newSnapshot;

                    if ( arrayIndex > 0 )
                    {
                        ReadOnlyMemory<T> memory = new( array, start: 0, length: arrayIndex );
                        bool isInitializing = m_items is ITrackable t ? t.IsInitializing : true;

                        ForwardOnNext( new NewItems<T>( memory, isInitializing: isInitializing ) );
                    }
                }
                else
                {
                    m_snapshot = newSnapshot;
                }
            }


            private HashSet<T> m_snapshot;
            private readonly IObservableEnumerable<T> m_items;
        }
    }
}
