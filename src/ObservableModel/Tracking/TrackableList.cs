using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    /// <summary>
    /// Implementation of a dynamic data collection based on ObservableObject that uses List&lt;T&gt; as internal container, 
    /// implementing INotifyCollectionChanged to notify listeners when items get added, removed or the whole list is refreshed.
    /// </summary>
    public class TrackableList<T> : TrackableCollection<T> where T : notnull
    {
        public TrackableList( IEnumerable<T>? items = null, IEqualityComparer<T>? comparer = null, bool supressItemTracking = false )
            : base( items, supressItemTracking )
        {
            Comparer = comparer ?? Equality.GetComparer<T>();
        }

        protected override IEqualityComparer<T> Comparer { get; }

        public override bool OriginalEquals( object? obj )
        {
            if ( obj is IReadOnlyList<T> items )
            {
                var originalItems = m_originalItems ?? (IReadOnlyList<T>)this;

                if ( originalItems.Count != items.Count )
                    goto NotEqual;

                for ( var i = 0; i < originalItems.Count; ++i )
                {
                    var originalItem = originalItems[ i ];
                    var item = items[ i ];

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

        public override IEnumerable<TValue> GetOriginalItems()
        {
            if ( m_originalItems is not null )
                return m_originalItems;

            return this;
        }

        public override IEnumerable<TrackableListChangedItem<T>> GetChangedItems()
        {
            if ( m_originalItems is null )
            {
                // No collection changes. Only possible changes are for ITrackable items.
                if ( IsItemTrackable )
                {
                    foreach ( var item in this )
                    {
                        if ( ( (ITrackable)item ).IsChanged )
                            yield return new TrackableListChangedItem<T>( TrackableListChangeType.Change, item, item );
                    }
                }

                yield break;
            }

            var comparer = IsItemTrackable ? ReferenceEqualityComparer<T>.Default : Comparer;
            var originalItems = new HashSet<T>( m_originalItems, comparer );

            for ( var index = 0; index < Count; ++index )
            {
                var item = this[ index ];
                var originalItem = m_originalItems.Length > index ? m_originalItems[ index ] : default;

                if ( comparer.Equals( item, originalItem ) )
                {
                    originalItems.Remove( item );

                    if ( IsItemTrackable && ( (ITrackable)item ).IsChanged )
                    {
                        yield return new TrackableListChangedItem<T>( TrackableListChangeType.Change, item, originalItem! );
                    }
                }
                else if ( !originalItems.Remove( item ) )
                {
                    yield return new TrackableListChangedItem<T>( TrackableListChangeType.Add, item, default! );
                }
                else
                {
                    // The item itself might not be changed, but its position has so we should return a change
                    yield return new TrackableListChangedItem<T>( TrackableListChangeType.Change, item, item );
                }
            }

            foreach ( var item in originalItems )
            {
                yield return new TrackableListChangedItem<T>( TrackableListChangeType.Remove, item, item );
            }
        }

        protected override void OnDiscardOriginal( out IEnumerable<T>? originalItems )
        {
            originalItems = m_originalItems;
            m_originalItems = null;
        }

        protected override void OnCreateOriginal()
        {
            if ( Count > 0 )
            {
                m_originalItems = new T[ Count ];
                CopyTo( m_originalItems, 0 );
            }
            else
            {
                m_originalItems = [];
            }
        }

        protected override bool TryFindOriginalTrackableItem( T current, [MaybeNullWhen( false )] out T original )
        {
            var items = m_originalItems;

            if ( items is null )
            {
                original = current;
                return true;
            }

            for ( var i = 0; i < items.Length; ++i )
            {
                if ( ReferenceEquals( items[ i ], current ) )
                {
                    original = current;
                    return true;
                }
            }

            original = default!;
            return false;
        }

        protected override void OnInsertOriginalItem( T item, int itemIndex )
        {
            Debug.Assert( m_originalItems is not null );

            var insertIndex = Math.Min( m_originalItems.Length - 1, itemIndex );

            for ( ; insertIndex > 0; --insertIndex )
            {
                if ( ReferenceEquals( this[ insertIndex - 1 ], m_originalItems[ insertIndex - 1 ] ) )
                    break;
            }

            var newOriginalItems = new T[ m_originalItems.Length + 1 ];

            if ( insertIndex > 0 )
                m_originalItems.AsSpan( 0, insertIndex ).CopyTo( newOriginalItems );

            if ( insertIndex < m_originalItems.Length )
                m_originalItems.AsSpan( insertIndex ).CopyTo( newOriginalItems.AsSpan( insertIndex + 1 ) );

            newOriginalItems[ insertIndex ] = item;
            m_originalItems = newOriginalItems;
        }

        protected override void OnRemoveOriginalItem( T item )
        {
            Debug.Assert( m_originalItems is not null );

            var removedItemIndex = -1;

            for ( var i = 0; i < m_originalItems.Length; ++i )
            {
                if ( ReferenceEquals( item, m_originalItems[ i ] ) )
                {
                    removedItemIndex = i;
                    break;
                }
            }

            if ( removedItemIndex >= 0 )
            {
                var newOriginalItems = new T[ m_originalItems.Length - 1 ];

                if ( removedItemIndex > 0 )
                    m_originalItems.AsSpan( 0, removedItemIndex ).CopyTo( newOriginalItems );

                if ( newOriginalItems.Length > removedItemIndex )
                    m_originalItems.AsSpan( removedItemIndex + 1 ).CopyTo( newOriginalItems.AsSpan( removedItemIndex ) );

                m_originalItems = newOriginalItems;
            }
        }

        private T[]? m_originalItems;
    }
}
