using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    /// <summary>
    /// Represents an observable collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class TrackableDictionary<TKey, TValue> : TrackableCollection<TValue>
        where TKey : notnull
        where TValue : notnull
    {
        public TrackableDictionary( Func<TValue, TKey> key,
                                    IEqualityComparer<TKey>? keyComparer = null,
                                    IEqualityComparer<TValue>? valueComparer = null,
                                    IEnumerable<TValue>? values = null,
                                    bool supressItemTracking = false )
            : base( values, supressItemTracking )
        {
            m_key = key ?? throw new ArgumentNullException( nameof( key ) );
            m_dictionary = new Dictionary<TKey, int>( keyComparer ?? EqualityComparer<TKey>.Default );

            Comparer = valueComparer ?? EqualityComparer<TValue>.Default;

            if ( values is object )
            {
                for ( var i = 0; i < Count; i++ )
                {
                    m_dictionary.Add( m_key( this[ i ] ), i );
                }
            }
        }

        protected override sealed IEqualityComparer<TValue> Comparer { get; }

        public Dictionary<TKey, int>.KeyCollection Keys => m_dictionary.Keys;

        public override bool OriginalEquals( object? obj )
        {
            if ( obj is IReadOnlyList<TValue> items )
            {
                if ( m_originalItems is null && obj == this && !IsItemTrackable )
                    return true;

                var original = m_originalItems is not null ? new Dictionary( m_originalItems ) : new Dictionary( this );

                if ( original.Count != items.Count )
                    goto NotEqual;

                for ( var i = 0; i < items.Count; ++i )
                {
                    var item = items[ i ];

                    if ( !original.TryGetValue( m_key( item ), out var originalItem ) )
                        goto NotEqual;

                    if ( IsItemTrackable )
                    {
                        if ( !( (ITrackable)originalItem ).OriginalEquals( item ) )
                            goto NotEqual;
                    }
                    else if ( !Comparer.Equals( originalItem, item ) )
                    {
                        goto NotEqual;
                    }
                }

                return true;
            }

        NotEqual:
            return false;
        }

        public override IEnumerable<TrackableListChangedItem<TValue>> GetChangedItems()
        {
            if ( m_originalItems is null )
            {
                // No collection changes. Only possible changes are for ITrackable items.
                if ( IsItemTrackable )
                {
                    foreach ( var item in this )
                    {
                        if ( ( (ITrackable)item ).IsChanged )
                            yield return new TrackableListChangedItem<TValue>( TrackableListChangeType.Change, item, item );
                    }
                }

                yield break;
            }

            var originalItems = new Dictionary<TKey, TValue>( m_originalItems, m_dictionary.Comparer );

            foreach ( var item in this )
            {
                if ( !originalItems.Remove( m_key( item ), out var originalItem ) )
                {
                    yield return new TrackableListChangedItem<TValue>( TrackableListChangeType.Add, item, default! );
                }
                else if ( IsItemTrackable && !( (ITrackable)originalItem ).OriginalEquals( item ) )
                {
                    yield return new TrackableListChangedItem<TValue>( TrackableListChangeType.Change, item, originalItem );
                }
            }

            foreach ( var originalItem in originalItems.Values )
            {
                yield return new TrackableListChangedItem<TValue>( TrackableListChangeType.Remove, originalItem, originalItem );
            }
        }

        /// <summary>
        ///  Determines whether the collection contains the specified key. 
        /// </summary>
        public bool ContainsKey( TKey key ) => m_dictionary.ContainsKey( key );

        public bool ContainsOriginalKey( TKey key )
        {
            if ( m_originalItems is not null )
                return m_originalItems.ContainsKey( key );

            return m_dictionary.ContainsKey( key );
        }

        /// <summary>
        /// Removes the value located at the specified key.
        /// </summary>
        /// <returns>True if the key existed, False when it didn't</returns>
        public bool RemoveKey( TKey key )
        {
            var index = IndexOfKey( key );

            if ( index > -1 )
            {
                RemoveAt( index );

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the value located at the specified key from the original collection.
        /// </summary>
        public bool RemoveOriginalKey( TKey key )
        {
            EnsureOriginal();

            if ( m_originalItems?.Remove( key ) == true )
            {
                UpdateChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the index of the provided key inside the current collection or -1 if not found.
        /// </summary>
        public int IndexOfKey( TKey key )
        {
            if ( !m_dictionary.TryGetValue( key, out var index ) )
            {
                index = -1;
            }

            return index;
        }

        /// <summary>
        /// Gets the value associated with the specified key. Returns True if the key was found, False otherwise.
        /// </summary>
        public bool TryGetValue( TKey key, [MaybeNullWhen( returnValue: false )] out TValue value )
        {
            var index = IndexOfKey( key );

            if ( index >= 0 )
            {
                value = this[ index ];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns the value associated with the specified key.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">the key doesn't exist</exception>
        public TValue GetValue( TKey key )
        {
            var index = IndexOfKey( key );

            if ( index < 0 )
            {
                throw new KeyNotFoundException( $"The key {key} was not found." );
            }

            return this[ index ];
        }

        /// <summary>
        /// Adds the value in case the key doesn't already exist in the dictionary
        /// or updates it otherwise.
        /// </summary>
        public void AddOrUpdate( TValue value )
        {
            var key = m_key( value );
            var index = IndexOfKey( key );

            if ( index > -1 )
            {
                this[ index ] = value;
            }
            else
            {
                Add( value );
            }
        }

        public void AddOrUpdateOriginal( TValue value )
        {
            var key = m_key( value );

            if ( m_originalItems is null )
            {
                // If the original item being addded is the same as the current, do nothing
                if ( TryGetValue( key, out var current ) && ReferenceEquals( value, current ) )
                    return;

                OnCreateOriginal();
            }

            var isChangedBefore = IsValueChanged( key );

            m_originalItems[ key ] = value;

            if ( isChangedBefore != IsValueChanged( key ) )
            {
                UpdateChanged();
            }
        }

        public override bool Contains( TValue item ) => Contains( item, Comparer );

        public override bool Contains( TValue item, IEqualityComparer<TValue> comparer )
            => TryGetValue( m_key( item ), out var existingItem ) && comparer.Equals( item, existingItem );

        public override int IndexOf( TValue item )
        {
            if ( m_dictionary.TryGetValue( m_key( item ), out var index ) && Comparer.Equals( item, this[ index ] ) )
                return index;

            return -1;
        }

        public bool IsValueChanged( TKey key ) => TryGetChange( key, out var _ );

        public bool TryGetChange( TKey key, out TrackableListChangedItem<TValue> change )
        {
            var originalValue = default( TValue );
            var hasOriginalValue = m_originalItems is not null
                                && m_originalItems.TryGetValue( key, out originalValue );

            if ( !TryGetValue( key, out var currentValue ) )
            {
                // If we don't have current value the key should be consired changed if
                // original value exists - i.e. the item change is removal.
                if ( hasOriginalValue )
                {
                    change = TrackableListChangedItem.Remove( originalValue! );
                    return true;
                }

                goto NotChanged;
            }

            if ( !hasOriginalValue )
            {
                if ( IsChanged && m_originalItems is not null )
                {
                    change = TrackableListChangedItem.Add( currentValue );
                    return true;
                }
                else if ( IsItemTrackable && ( (ITrackable)currentValue ).IsChanged )
                {
                    change = TrackableListChangedItem.Change( currentValue, currentValue );
                    return true;
                }

                goto NotChanged;
            }

            Debug.Assert( originalValue is object );

            var same = IsItemTrackable ?
                ( (ITrackable)originalValue ).OriginalEquals( currentValue ) :
                Comparer.Equals( originalValue, currentValue );

            if ( !same )
            {
                change = TrackableListChangedItem.Change( currentValue, originalValue );
                return true;
            }

        NotChanged:
            change = default;
            return false;
        }

        public TKey GetKey( TValue value ) => m_key( value );

        protected override void OnClear()
        {
            m_dictionary.Clear();

            base.OnClear();
        }

        protected override void OnReset( List<TValue> items )
        {
            m_dictionary.Clear();

            for ( var i = 0; i < items.Count; ++i )
            {
                m_dictionary.Add( m_key( items[ i ] ), i );
            }

            base.OnReset( items );
        }

        protected override void OnMove( TValue item, int oldIndex, int newIndex )
        {
            m_dictionary[ m_key( item ) ] = newIndex;

            if ( newIndex > oldIndex )
            {
                // The item is moved to the back of the collection causing
                // all items between to move 1 position forward
                for ( var i = oldIndex + 1; i <= newIndex; ++i )
                {
                    m_dictionary[ m_key( this[ i ] ) ] -= 1;
                }
            }
            else
            {
                // The item is moved to the front of the collection causing
                // all items between to move 1 position backward
                for ( var i = newIndex; i < oldIndex; ++i )
                {
                    m_dictionary[ m_key( this[ i ] ) ] += 1;
                }
            }

            base.OnMove( item, oldIndex, newIndex );
        }

        protected override void OnSorted()
        {
            // Reset all indexes
            for ( var i = 0; i < Count; ++i )
            {
                m_dictionary[ m_key( this[ i ] ) ] = i;
            }

            base.OnSorted();
        }

        protected override void OnReplace( TValue oldItem, TValue newItem, int index )
        {
            var newItemKey = m_key( newItem );

            m_dictionary.Remove( m_key( oldItem ) );
            m_dictionary.Add( newItemKey, index );

            // If the original items are already created we check if the new item is the
            // same as the original one, and in that case - store it back in the original dictionary.
            if ( m_originalItems is not null && !TryGetChange( newItemKey, out var _ ) )
            {
                m_originalItems[ newItemKey ] = newItem;
            }

            base.OnReplace( oldItem, newItem, index );
        }

        protected override void OnRemove( TValue item, int index )
        {
            m_dictionary.Remove( m_key( item ) );

            for ( var i = index + 1; i < Count; ++i )
            {
                m_dictionary[ m_key( this[ i ] ) ] -= 1;
            }

            base.OnRemove( item, index );
        }

        protected override void OnInsert( TValue item, int index )
        {
            m_dictionary.Add( m_key( item ), index );

            for ( var i = index; i < Count; ++i )
            {
                m_dictionary[ m_key( this[ i ] ) ] += 1;
            }

            base.OnInsert( item, index );
        }

        protected override bool TryFindOriginalTrackableItem( TValue current, [MaybeNullWhen( returnValue: false )] out TValue original )
        {
            if ( m_originalItems is null )
            {
                original = current;
                return true;
            }

            return m_originalItems.TryGetValue( m_key( current ), out original );
        }

        [MemberNotNull( nameof( m_originalItems ) )]
        protected override void OnCreateOriginal()
        {
            if ( Count > 0 )
            {
                m_originalItems = new Dictionary<TKey, TValue>( capacity: Count, m_dictionary.Comparer );

                foreach ( var item in this )
                {
                    m_originalItems.Add( m_key( item ), item );
                }
            }
            else
            {
                m_originalItems = [];
            }
        }

        protected override void OnDiscardOriginal( out IEnumerable<TValue>? originalItems )
        {
            originalItems = m_originalItems?.Values;
            m_originalItems = null;
        }

        protected override void OnInsertOriginalItem( TValue item, int itemIndex )
        {
            Debug.Assert( m_originalItems is not null );

            m_originalItems[ m_key( item ) ] = item;
        }

        protected override void OnRemoveOriginalItem( TValue item )
        {
            Debug.Assert( m_originalItems is not null );

            m_originalItems.Remove( m_key( item ) );
        }

        private readonly Func<TValue, TKey> m_key;
        private readonly Dictionary<TKey, int> m_dictionary;

        private Dictionary<TKey, TValue>? m_originalItems;

        private readonly ref struct Dictionary
        {
            public Dictionary( Dictionary<TKey, TValue> source )
            {
                m_dictionary = source;
                m_collection = null;
            }

            public Dictionary( TrackableDictionary<TKey, TValue> source )
            {
                m_collection = source;
                m_dictionary = null;
            }

            public int Count => m_dictionary?.Count ?? m_collection!.Count;

            public bool TryGetValue( TKey key, [MaybeNullWhen( returnValue: false )] out TValue value )
            {
                if ( m_dictionary is not null )
                    return m_dictionary.TryGetValue( key, out value );

                return m_collection!.TryGetValue( key, out value );
            }

            private readonly Dictionary<TKey, TValue>? m_dictionary;
            private readonly TrackableDictionary<TKey, TValue>? m_collection;
        }
    }
}
