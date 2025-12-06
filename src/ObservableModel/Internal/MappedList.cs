using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ObservableModel
{
    internal sealed class MappedList<TSource, TResult> : IReadOnlyObservableList<TResult>,
                                                         IObserver<NotifyCollectionChangedEventArgs>
    {
        public MappedList( IObservableEnumerable<TSource> source, Func<TSource, TResult> factory )
        {
            m_items = [];
            m_factory = factory;
            m_source = new WeakReference<IObservableEnumerable<TSource>>( source );

            Reset( source );
            source.CollectionChanges.SubscribeWeak( this );
        }


        [MaybeNull]
        public TResult First => m_items.First;

        [MaybeNull]
        public TResult Last => m_items.Last;


        public int Count => m_items.Count;


        public bool SupportsItemsChanges => false;


        public bool IsEmpty => m_items.IsEmpty;


        public IObservable<ObservablePropertyChange> ItemsChanges => throw new NotSupportedException();


        public IObservable<NotifyCollectionChangedEventArgs> CollectionChanges => m_items.CollectionChanges;


        public TResult this[ int index ] => m_items[ index ];


        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => m_items.PropertyChanged += value;
            remove => m_items.PropertyChanged -= value;
        }


        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => m_items.CollectionChanged += value;
            remove => m_items.CollectionChanged -= value;
        }


        public IEnumerator<TResult> GetEnumerator() => ( (IReadOnlyObservableList<TResult>)m_items ).GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => ( (IReadOnlyObservableList<TResult>)m_items ).GetEnumerator();


        private void Reset( IObservableEnumerable<TSource> source ) => m_items.Reset( source.Select( m_factory ) );


        public void OnCompleted()
        {
        }


        public void OnError( Exception error )
        {
        }


        public void OnNext( NotifyCollectionChangedEventArgs e )
        {
            if ( !m_source.TryGetTarget( out var source ) )
                return;

            switch ( e.Action )
            {
                case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; i++ )
                    {
                        m_items.Insert( e.NewStartingIndex + i, m_factory( (TSource)e.NewItems[ i ]! ) );
                    }
                    break;

                case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                    for ( var i = 0; i < e.OldItems.Count; i++ )
                    {
                        m_items.RemoveAt( e.OldStartingIndex );
                    }
                    break;

                case NotifyCollectionChangedAction.Move when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items.RemoveAt( e.OldStartingIndex + i );
                        m_items.Insert( e.NewStartingIndex + i, m_factory( (TSource)e.NewItems[ i ]! ) );
                    }
                    break;

                case NotifyCollectionChangedAction.Replace when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items[ e.NewStartingIndex + i ] = m_factory( (TSource)e.NewItems[ i ]! );
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Reset( source );
                    break;
            }
        }


        private readonly Func<TSource, TResult> m_factory;

        private readonly ObservableList<TResult> m_items;
        private readonly WeakReference<IObservableEnumerable<TSource>> m_source;
    }
}
