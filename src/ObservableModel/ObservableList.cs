using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ObservableModel.Subjects;

namespace ObservableModel
{
    /// <summary>
    /// Implementation of a dynamic data collection based on ObservableObject that uses List&lt;T&gt; as internal container, 
    /// implementing INotifyCollectionChanged to notify listeners when items get added, removed or the whole list is refreshed.
    /// </summary>
    [DebuggerDisplay( "Count = {Count}" )]
    [DebuggerTypeProxy( typeof( ObservableList<>.DebugView ) )]
    public class ObservableList<T> : ObservableObject, IList<T>, IReadOnlyObservableList<T>, IObservableList, IObservableEnumerable<T>
    {
        private static readonly bool IsItemNotifyPropertyChanged = typeof( INotifyPropertyChanged ).IsAssignableFrom( typeof( T ) );


        public ObservableList()
        {
            m_items = [];
        }


        public ObservableList( int capacity )
        {
            m_items = new List<T>( capacity );
        }


        public ObservableList( IEnumerable<T>? collection )
        {
            m_items = collection is object ? [ .. collection ] : [];

            if ( m_items.Count > 0 )
            {
                foreach ( var item in m_items )
                {
                    ConnectPropertyChanged( item );
                }

                IsEmpty = false;
            }
        }


        /// <summary>
        /// Event for notifying about changes in the list such as Add, Remove, Reset, Move.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;


        /// <summary>
        /// An observable for the CollectionChanged event.
        /// </summary>
        public IObservable<NotifyCollectionChangedEventArgs> CollectionChanges => m_collectionChanges ??= new Subject<NotifyCollectionChangedEventArgs>();


        public bool SupportsItemsChanges => IsItemNotifyPropertyChanged;


        /// <summary>
        /// An observable for changes in the items contained in this list.
        /// </summary>
        /// <exception cref="NotSupportedException">when the underlying item doesn't implement INotifyPropertyChanged</exception>
        public IObservable<ObservablePropertyChange<T>> ItemsChanges
        {
            get
            {
                if ( m_itemsChanges is null )
                    CreateItemsChanges();

                return m_itemsChanges!;
            }
        }


        IObservable<ObservablePropertyChange> IObservableEnumerable.ItemsChanges => ItemsChanges.Select( x => new ObservablePropertyChange( x.Source!, x.PropertyChangedEventArgs ) );


        /// <summary>
        /// Returns how many elements are in the list.
        /// </summary>
        public int Count => m_items.Count;


        public bool IsEmpty
        {
            get => m_empty;
            private set
            {
                if ( m_empty != value )
                {
                    m_empty = value;
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsEmpty );
                }
            }
        }

        /// <summary>
        /// Gets or sets the first item in the list. If the list is empty, the getter will return default( T ).
        /// The setter will add the value to the collection if the list is empty or replace the first item.
        /// </summary>
        [MaybeNull]
        public T First
        {
            get
            {
                if ( m_items.Count > 0 )
                {
                    return m_items[ 0 ];
                }

                return default!;
            }
            set
            {
                if ( m_items.Count == 0 )
                {
                    Add( value );
                }
                else
                {
                    this[ 0 ] = value;
                }
            }
        }


        /// <summary>
        /// Gets or sets the last item in the list. If the list is empty, the getter will return default( T ).
        /// The setter will add the value to the collection if the list is empty or replace the last item.
        /// </summary>
        [MaybeNull]
        public T Last
        {
            get
            {
                if ( m_items.Count > 0 )
                {
                    return m_items[ ^1 ];
                }

                return default!;
            }
            set
            {
                if ( m_items.Count == 0 )
                {
                    Add( value );
                }
                else
                {
                    this[ m_items.Count - 1 ] = value;
                }
            }
        }


        public T this[ int index ]
        {
            get => m_items[ index ];
            set
            {
                var originalItem = m_items[ index ];

                if ( Equality.IsDifferent( originalItem, value ) )
                {
                    OnReplace( originalItem, value, index );
                }
            }
        }


        public void Add( T item )
        {
            var index = m_items.Count;

            if ( m_persistedSort is not null )
            {
                index = m_items.BinarySearch( 0, index, item, m_persistedSort );

                if ( index < 0 )
                {
                    index = ~index;
                }
                else
                {
                    while ( index < m_items.Count )
                    {
                        if ( m_persistedSort.Compare( item, m_items[ index ] ) == 0 )
                        {
                            index += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            OnInsert( item, index );
        }


        public void AddRange( IEnumerable<T> items )
        {
            foreach ( var item in items )
            {
                Add( item );
            }
        }


        public void Clear()
        {
            if ( m_items.Count > 0 )
            {
                OnClear();
            }
        }


        public void Reset( IEnumerable<T> items )
        {
            if ( m_persistedSort is not null )
            {
                items = items.OrderBy( static x => x, m_persistedSort );
            }

            var itemsList = items.ToList();

            if ( itemsList.Count > 0 || Count > 0 )
            {
                OnReset( itemsList );
            }
        }


        public void CopyTo( T[] array, int index ) => m_items.CopyTo( array, index );


        public virtual bool Contains( T item ) => m_items.Contains( item );


        public virtual bool Contains( T item, IEqualityComparer<T> comparer ) => m_items.Contains( item, comparer );


        public List<T>.Enumerator GetEnumerator() => m_items.GetEnumerator();


        public virtual int IndexOf( T item ) => m_items.IndexOf( item );


        public void Insert( int index, T item )
        {
            if ( index < 0 || index > m_items.Count )
            {
                throw new ArgumentOutOfRangeException( nameof( index ) );
            }

            OnInsert( item, index );
        }


        public bool Remove( T item )
        {
            var index = m_items.IndexOf( item );

            if ( index > -1 )
            {
                RemoveAt( index );

                return true;
            }

            return false;
        }


        public void RemoveAt( int index ) => OnRemove( m_items[ index ], index );


        public void Move( int oldIndex, int newIndex )
        {
            if ( oldIndex < 0 || oldIndex >= m_items.Count )
            {
                throw new ArgumentOutOfRangeException( nameof( oldIndex ) );
            }
            else if ( newIndex < 0 || newIndex >= m_items.Count )
            {
                throw new ArgumentOutOfRangeException( nameof( newIndex ) );
            }

            if ( oldIndex != newIndex )
            {
                OnMove( m_items[ oldIndex ], oldIndex, newIndex );
            }
        }


        public void ForEach( Action<T> action )
        {
            foreach ( var item in m_items )
            {
                action( item );
            }
        }


        /// <summary>
        /// Updates the position of the provided item in the current collection based
        /// on the persisted sorting expression.
        /// </summary>
        public void UpdateSortPosition( T item )
        {
            if ( m_persistedSort is null )
                throw new InvalidOperationException( "The collection doesn't have persisted sorting expression." );

            int currentIndex = IndexOf( item );
            int startIndex;
            int count;

            if ( currentIndex > 0 && m_persistedSort.Compare( this[ currentIndex - 1 ], item ) > 0 )
            {
                // The previous item is "bigger" than the current, so the current needs to be before it
                startIndex = 0;
                count = currentIndex;
            }
            else if ( currentIndex < m_items.Count - 1 && m_persistedSort.Compare( item, this[ currentIndex + 1 ] ) > 0 )
            {
                // The current item is "bigger" than the next, so the current item needs to be after it
                startIndex = currentIndex + 1;
                count = Count - startIndex;
            }
            else
            {
                // The position didn't change
                return;
            }

            var newIndex = m_items.BinarySearch( startIndex, count, item, m_persistedSort );

            if ( newIndex < 0 )
                newIndex = ~newIndex;

            if ( newIndex > currentIndex )
                newIndex -= 1;

            if ( currentIndex != newIndex )
                Move( currentIndex, newIndex );
        }


        public void Sort()
        {
            if ( m_persistedSort is not null )
            {
                Sort( m_persistedSort.Comparison, persist: true );
            }
            else
            {
                Sort( Comparer<T>.Default.Compare, persist: false );
            }
        }


        public void Sort( Comparison<T> comparison, bool persist = false )
        {
            OnSorting();

            // The default sort algorithm in .NET is unstable. We need to know the indexes of each item
            // so the same items should appear in the same order as they were originally.
            var indexes = new int[ m_items.Count ];
            var values = new T[ m_items.Count ];

            for ( var i = 0; i < m_items.Count; ++i )
            {
                indexes[ i ] = i;
                values[ i ] = m_items[ i ];
            }

            MemoryExtensions.Sort( keys: indexes.AsSpan(), items: CollectionsMarshal.AsSpan( m_items ), ( x, y ) =>
            {
                var result = comparison( values[ x ], values[ y ] );

                if ( result == 0 )
                    return x.CompareTo( y );

                return result;
            } );

            OnSorted();

            m_persistedSort = persist ? new ComparisonComparer( comparison ) : null;
        }


        public void SortBy<TKey>( Func<T, TKey> key, bool persist = false ) where TKey : IComparable<TKey>
            => Sort( new Comparison<T>( ( x, y ) => key( x ).CompareTo( key( y ) ) ), persist );


        /// <summary>
        /// This method removes all items which matches the predicate.
        /// </summary>
        public int RemoveAll( Predicate<T> predicate )
        {
            var removeCount = 0;

            for ( var i = Count - 1; i >= 0; --i )
            {
                if ( predicate( m_items[ i ] ) )
                {
                    RemoveAt( i );

                    ++removeCount;
                }
            }

            return removeCount;
        }


        /// <summary>
        /// Maps the current collection onto the target collection without preserving item position inside the target.
        /// </summary>
        public IDisposable Bind<TResult>( ICollection<TResult> target, Func<T, TResult> selector )
        {
            // We need to keep track of the original items of the collection in case of a reset
            var originalItems = target.Count > 0 ? target.ToArray() : [];

            // Populate the target items
            foreach ( var item in m_items )
            {
                target.Add( selector( item ) );
            }

            return CollectionChanges.Subscribe( x =>
            {
                switch ( x.Action )
                {
                    case NotifyCollectionChangedAction.Add when x.NewItems is not null:
                        foreach ( var item in x.NewItems )
                        {
                            target.Add( selector( (T)item! ) );
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove when x.OldItems is not null:
                        foreach ( var item in x.OldItems )
                        {
                            target.Remove( selector( (T)item! ) );
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace when x.NewItems is not null && x.OldItems is not null:
                        foreach ( var item in x.OldItems )
                        {
                            target.Remove( selector( (T)item! ) );
                        }

                        foreach ( var item in x.NewItems )
                        {
                            target.Add( selector( (T)item! ) );
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        target.Clear();

                        foreach ( var originalItem in originalItems )
                        {
                            target.Add( originalItem );
                        }

                        foreach ( var item in m_items )
                        {
                            target.Add( selector( item ) );
                        }
                        break;
                }
            } );
        }


        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is
        /// used as the initial accumulator value.
        /// </summary>
        public ObservableAggregate<TAccumulate> Aggregate<TAccumulate>( TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, [CallerArgumentExpression( nameof( func ) )] string? funcExpression = null )
        {
            var aggregate = new ObservableAggregate<T, TAccumulate>( this, seed, func, funcExpression );

            m_aggregates ??= [];
            m_aggregates.Add( aggregate );

            if ( IsItemNotifyPropertyChanged && m_itemsChanges is null )
            {
                CreateItemsChanges();
            }

            return aggregate;
        }


        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public ObservableAggregate<TAccumulate> Aggregate<TAccumulate>( Func<TAccumulate, T, TAccumulate> func, [CallerArgumentExpression( nameof( func ) )] string? funcExpression = null )
            => Aggregate( default!, func, funcExpression );


        protected virtual void OnClear()
        {
            foreach ( var item in m_items )
            {
                DisconnectPropertyChanged( item );
            }

            m_items.Clear();
            IsEmpty = true;

            OnCollectionChanged();
            RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Count );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionReset();
        }


        protected virtual void OnReset( List<T> items )
        {
            foreach ( var item in m_items )
            {
                DisconnectPropertyChanged( item );
            }

            foreach ( var item in items )
            {
                ConnectPropertyChanged( item );
            }

            m_items = items;
            IsEmpty = items.Count == 0;

            OnCollectionChanged();
            RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Count );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionReset();
        }


        /// <summary>
        /// Called before inserting an item in the collection.
        /// </summary>
        /// <param name="item">The item being inserted.</param>
        /// <param name="index">The index where the item will be inserted.</param>
        protected virtual void OnInsert( T item, int index )
        {
            m_items.Insert( index, item );

            ConnectPropertyChanged( item );
            OnCollectionChanged();

            if ( index == 0 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            }
            else if ( index == m_items.Count - 1 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            }

            RaisePropertyChanged( SharedPropertyChangedEventArgs.Count );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionChanged( NotifyCollectionChangedAction.Add, item, index );
        }


        protected virtual void OnReplace( T oldItem, T newItem, int index )
        {
            DisconnectPropertyChanged( oldItem );
            ConnectPropertyChanged( newItem );

            m_items[ index ] = newItem;

            OnCollectionChanged();

            if ( index == 0 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            }
            else if ( index == m_items.Count - 1 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            }

            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionChanged( NotifyCollectionChangedAction.Replace, oldItem, newItem, index );
        }


        /// <summary>
        /// Called before removing an item from the collection.
        /// </summary>
        protected virtual void OnRemove( T item, int index )
        {
            m_items.RemoveAt( index );

            DisconnectPropertyChanged( item );
            OnCollectionChanged();

            if ( index == 0 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            }
            else if ( index == m_items.Count - 2 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            }

            RaisePropertyChanged( SharedPropertyChangedEventArgs.Count );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionChanged( NotifyCollectionChangedAction.Remove, item, index );
        }


        /// <summary>
        /// Called before moving an item from the collection.
        /// </summary>
        protected virtual void OnMove( T item, int oldIndex, int newIndex )
        {
            m_items.RemoveAt( oldIndex );
            m_items.Insert( newIndex, item );

            OnCollectionChanged();

            if ( oldIndex == 0 || newIndex == 0 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            }

            if ( oldIndex == m_items.Count - 1 || newIndex == m_items.Count - 1 )
            {
                RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            }

            RaisePropertyChanged( SharedPropertyChangedEventArgs.Indexer );
            RaiseCollectionChanged( NotifyCollectionChangedAction.Move, item, newIndex, oldIndex );
        }


        /// <summary>
        /// Called before the collection is sorted.
        /// </summary>
        protected virtual void OnSorting()
        {
        }


        /// <summary>
        /// Called after the collection is sorted.
        /// </summary>
        protected virtual void OnSorted()
        {
            OnCollectionChanged();
            RaisePropertyChanged( SharedPropertyChangedEventArgs.First );
            RaisePropertyChanged( SharedPropertyChangedEventArgs.Last );
            RaiseCollectionReset();
        }


        protected virtual void OnCollectionChanged()
        {
        }


        protected void OnItemPropertyChanged( object? sender, PropertyChangedEventArgs e )
        {
            if ( sender is null )
                return;

            m_itemsChanges!.OnNext( new ObservablePropertyChange<T>( (T)sender, e ) );
            UpdateAggregates( e );
        }


        private void ConnectPropertyChanged( T item )
        {
            if ( m_itemsChanges is not null && item is INotifyPropertyChanged npc )
            {
                npc.PropertyChanged += m_itemPropertyChangedHandler;
            }
        }


        private void DisconnectPropertyChanged( T item )
        {
            if ( m_itemsChanges is not null && item is INotifyPropertyChanged npc )
            {
                npc.PropertyChanged -= m_itemPropertyChangedHandler;
            }
        }


        bool ICollection<T>.IsReadOnly => false;


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_items.GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => ( (IEnumerable)m_items ).GetEnumerator();


        bool ICollection.IsSynchronized => false;


        object ICollection.SyncRoot => ( (ICollection)m_items ).SyncRoot;


        void ICollection.CopyTo( Array array, int index ) => ( (ICollection)m_items ).CopyTo( array, index );


        object? IList.this[ int index ]
        {
            get => m_items[ index ];
            set => this[ index ] = (T)( value ?? throw new ArgumentNullException( nameof( value ) ) );
        }


        bool IList.IsReadOnly => false;


        bool IList.IsFixedSize => false;


        int IList.Add( object? item )
        {
            ArgumentNullException.ThrowIfNull( item );

            var index = Count;

            Add( (T)item );

            return index;
        }


        bool IList.Contains( object? item ) => item is not null && Contains( (T)item );


        void IList.Insert( int index, object? item ) => Insert( index, (T)( item ?? throw new ArgumentNullException( nameof( item ) ) ) );


        int IList.IndexOf( object? item ) => item is null ? -1 : IndexOf( (T)item );


        void IList.Remove( object? item )
        {
            if ( item is not null )
            {
                Remove( (T)item );
            }
        }


        protected void RaiseCollectionChanged( NotifyCollectionChangedAction action, T item, int index )
        {
            var handler = CollectionChanged;
            NotifyCollectionChangedEventArgs? args = null;

            if ( handler is not null )
            {
                handler( this, args = new NotifyCollectionChangedEventArgs( action, item, index ) );
            }

            m_collectionChanges?.OnNext( args ?? new NotifyCollectionChangedEventArgs( action, item, index ) );

            IsEmpty = m_items.Count == 0;
            UpdateAggregates();
        }


        protected void RaiseCollectionChanged( NotifyCollectionChangedAction action, T oldItem, T newItem, int index )
        {
            var handler = CollectionChanged;
            NotifyCollectionChangedEventArgs? args = null;

            if ( handler is not null )
            {
                handler( this, args = new NotifyCollectionChangedEventArgs( action, newItem, oldItem, index ) );
            }

            m_collectionChanges?.OnNext( args ?? new NotifyCollectionChangedEventArgs( action, newItem, oldItem, index ) );

            UpdateAggregates();
        }


        protected void RaiseCollectionChanged( NotifyCollectionChangedAction action, T oldItem, int index, int oldIndex )
        {
            var handler = CollectionChanged;
            NotifyCollectionChangedEventArgs? args = null;

            if ( handler is not null )
            {
                handler( this, args = new NotifyCollectionChangedEventArgs( action, oldItem, index, oldIndex ) );
            }

            m_collectionChanges?.OnNext( args ?? new NotifyCollectionChangedEventArgs( action, oldItem, index, oldIndex ) );

            UpdateAggregates();
        }


        protected void RaiseCollectionReset()
        {
            CollectionChanged?.Invoke( this, SharedPropertyChangedEventArgs.CollectionReset );
            m_collectionChanges?.OnNext( SharedPropertyChangedEventArgs.CollectionReset );

            UpdateAggregates();
        }


        private void UpdateAggregates( PropertyChangedEventArgs? e = null )
        {
            if ( m_aggregates is not null )
            {
                foreach ( var aggregate in m_aggregates )
                {
                    aggregate.RaiseMaybeChanged( e );
                }
            }
        }


        private void CreateItemsChanges()
        {
            if ( !IsItemNotifyPropertyChanged )
            {
                Debugger.NotifyOfCrossThreadDependency();

                throw new NotSupportedException( $"Items changes are not supported because the underlying type {typeof( T )} doesn't implemented INotifyPropertyChanged." );
            }

            m_itemPropertyChangedHandler = new PropertyChangedEventHandler( OnItemPropertyChanged );
            m_itemsChanges = new Subject<ObservablePropertyChange<T>>();

            foreach ( var item in m_items )
            {
                ConnectPropertyChanged( item );
            }
        }


        private List<T> m_items;
        private Subject<ObservablePropertyChange<T>>? m_itemsChanges;
        private PropertyChangedEventHandler? m_itemPropertyChangedHandler;
        private Subject<NotifyCollectionChangedEventArgs>? m_collectionChanges;

        private List<IObservableAggregate>? m_aggregates;

        private ComparisonComparer? m_persistedSort;
        private bool m_empty = true;


        private sealed class DebugView
        {
            public DebugView( ObservableList<T> list ) => m_list = list;

            [DebuggerBrowsable( DebuggerBrowsableState.RootHidden )]
            public T[] Items
            {
                get
                {
                    var items = new T[ m_list.Count ];
                    m_list.CopyTo( items, 0 );

                    return items;
                }
            }

            private readonly ObservableList<T> m_list;
        }

        private sealed class ComparisonComparer : Comparer<T>
        {
            public ComparisonComparer( Comparison<T> comparison )
            {
                Comparison = comparison;
            }

            public Comparison<T> Comparison { get; }

            public override int Compare( T? x, T? y ) => Comparison( x!, y! );
        }
    }
}
