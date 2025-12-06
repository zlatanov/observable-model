using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ObservableModel.Subjects;

namespace ObservableModel
{
    internal sealed class CombinedList<T> : IReadOnlyObservableList<T>
    {
        public CombinedList( IObservableEnumerable<T> first, IObservableEnumerable<T> second )
        {
            m_items = new ObservableList<T>( capacity: first.Count() + second.Count() );
            m_items.AddRange( first );
            m_secondStartIndex = m_items.Count;
            m_items.AddRange( second );

            first.CollectionChanged += OnFirstCollectionChanged;
            second.CollectionChanged += OnSecondCollectionChanged;
        }


        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => m_items.PropertyChanged += value;
            remove => m_items.PropertyChanged -= value;
        }


        public event NotifyCollectionChangedEventHandler? CollectionChanged;


        public int Count => m_items.Count;


        public T this[ int index ] => m_items[ index ];


        [MaybeNull]
        public T First => m_items.First;


        [MaybeNull]
        public T Last => m_items.Last;


        public bool SupportsItemsChanges => m_items.SupportsItemsChanges;


        public bool IsEmpty => m_items.IsEmpty;


        public IObservable<ObservablePropertyChange> ItemsChanges => ( (IReadOnlyObservableList<T>)m_items ).ItemsChanges;


        public IObservable<NotifyCollectionChangedEventArgs> CollectionChanges => m_collectionChanges ??= new Subject<NotifyCollectionChangedEventArgs>();


        public List<T>.Enumerator GetEnumerator() => m_items.GetEnumerator();


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ( (IReadOnlyObservableList<T>)m_items ).GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => ( (IReadOnlyObservableList<T>)m_items ).GetEnumerator();


        private void OnFirstCollectionChanged( object? sender, NotifyCollectionChangedEventArgs e )
        {
            if ( sender is null )
                return;

            switch ( e.Action )
            {
                case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; i++ )
                    {
                        m_secondStartIndex += 1;
                        m_items.Insert( e.NewStartingIndex + i, (T)e.NewItems[ i ]! );
                    }
                    break;

                case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                    for ( var i = 0; i < e.OldItems.Count; i++ )
                    {
                        m_secondStartIndex -= 1;
                        m_items.RemoveAt( e.OldStartingIndex );
                    }
                    break;

                case NotifyCollectionChangedAction.Move when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items.RemoveAt( e.OldStartingIndex + i );
                        m_items.Insert( e.NewStartingIndex + i, (T)e.NewItems[ i ]! );
                    }
                    break;

                case NotifyCollectionChangedAction.Replace when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items[ e.NewStartingIndex + i ] = (T)e.NewItems[ i ]!;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    while ( m_secondStartIndex > 0 )
                    {
                        m_items.RemoveAt( --m_secondStartIndex );
                    }

                    foreach ( var item in (IEnumerable<T>)sender )
                    {
                        m_items.Insert( m_secondStartIndex++, item );
                    }
                    break;
            }

            RaiseCollectionChanged( e );
        }


        private void OnSecondCollectionChanged( object? sender, NotifyCollectionChangedEventArgs e )
        {
            if ( sender is null )
                return;

            switch ( e.Action )
            {
                case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; i++ )
                    {
                        m_items.Insert( m_secondStartIndex + e.NewStartingIndex + i, (T)e.NewItems[ i ]! );
                    }

                    RaiseCollectionChanged( new NotifyCollectionChangedEventArgs(
                        action: NotifyCollectionChangedAction.Add,
                        changedItems: e.NewItems,
                        startingIndex: m_secondStartIndex + e.NewStartingIndex ) );
                    break;

                case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                    for ( var i = 0; i < e.OldItems.Count; i++ )
                    {
                        m_items.RemoveAt( m_secondStartIndex + e.OldStartingIndex );
                    }

                    RaiseCollectionChanged( new NotifyCollectionChangedEventArgs(
                        action: NotifyCollectionChangedAction.Remove,
                        changedItems: e.OldItems,
                        startingIndex: m_secondStartIndex + e.OldStartingIndex ) );
                    break;

                case NotifyCollectionChangedAction.Move when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items.RemoveAt( m_secondStartIndex + e.OldStartingIndex + i );
                        m_items.Insert( m_secondStartIndex + e.NewStartingIndex + i, (T)e.NewItems[ i ]! );
                    }

                    RaiseCollectionChanged( new NotifyCollectionChangedEventArgs(
                        action: NotifyCollectionChangedAction.Move,
                        changedItems: e.NewItems,
                        index: m_secondStartIndex + e.NewStartingIndex,
                        oldIndex: m_secondStartIndex + e.OldStartingIndex ) );
                    break;

                case NotifyCollectionChangedAction.Replace when e.NewItems is not null:
                    for ( var i = 0; i < e.NewItems.Count; ++i )
                    {
                        m_items[ m_secondStartIndex + e.NewStartingIndex + i ] = (T)e.NewItems[ i ]!;
                    }

                    RaiseCollectionChanged( new NotifyCollectionChangedEventArgs(
                        action: NotifyCollectionChangedAction.Replace,
                        changedItems: e.NewItems,
                        startingIndex: m_secondStartIndex + e.NewStartingIndex ) );
                    break;

                case NotifyCollectionChangedAction.Reset:
                    while ( m_secondStartIndex < m_items.Count )
                    {
                        m_items.RemoveAt( m_items.Count - 1 );
                    }

                    foreach ( var item in (IEnumerable<T>)sender )
                    {
                        m_items.Add( item );
                    }

                    RaiseCollectionChanged( e );
                    break;
            }
        }


        private void RaiseCollectionChanged( NotifyCollectionChangedEventArgs e )
        {
            m_collectionChanges?.OnNext( e );
            CollectionChanged?.Invoke( this, e );
        }


        private readonly ObservableList<T> m_items;
        private Subject<NotifyCollectionChangedEventArgs>? m_collectionChanges;
        private int m_secondStartIndex;
    }
}
