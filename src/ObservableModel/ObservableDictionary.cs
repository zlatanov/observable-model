using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    /// <summary>
    /// Represents an observable collection of keys and values.
    /// </summary>
    public class ObservableDictionary<TKey, TValue> : ObservableList<TValue> where TKey : notnull
    {
        public ObservableDictionary( Func<TValue, TKey> keySelector ) :
            this( keySelector, EqualityComparer<TKey>.Default )
        {
        }

        public ObservableDictionary( Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer )
        {
            m_key = keySelector;
            m_dictionary = new Dictionary<TKey, int>( comparer );
        }

        public Dictionary<TKey, int>.KeyCollection Keys => m_dictionary.Keys;

        /// <summary>
        /// Determines whether the collection contains the specified key. 
        /// </summary>
        public bool ContainsKey( TKey key ) => m_dictionary.ContainsKey( key );

        /// <summary>
        /// Removes the value located at the specified key.
        /// </summary>
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
        /// <exception cref="KeyNotFoundException">the key doesn't exist</exception>
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

        public TKey GetKey( TValue value ) => m_key( value );

        public override bool Contains( TValue item ) => Contains( item, EqualityComparer<TValue>.Default );

        public override bool Contains( TValue item, IEqualityComparer<TValue> comparer )
            => TryGetValue( m_key( item ), out var existingItem ) && comparer.Equals( item, existingItem );

        public override int IndexOf( TValue item )
        {
            if ( m_dictionary.TryGetValue( m_key( item ), out var index )
              && EqualityComparer<TValue>.Default.Equals( item, this[ index ] ) )
                return index;

            return -1;
        }

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
            m_dictionary.Remove( m_key( oldItem ) );
            m_dictionary.Add( m_key( newItem ), index );

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

        private readonly Func<TValue, TKey> m_key;
        private readonly Dictionary<TKey, int> m_dictionary;
    }
}
