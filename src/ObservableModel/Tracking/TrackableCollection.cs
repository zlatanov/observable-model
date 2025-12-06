using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    public abstract class TrackableCollection<T> : ObservableList<T>, ITrackableList where T : notnull
    {
        protected TrackableCollection( IEnumerable<T>? items = null, bool supressItemTracking = false ) : base( items )
        {
            IsItemTrackable = !supressItemTracking && typeof( T ).IsAssignableTo( typeof( ITrackable ) );

            if ( IsItemTrackable )
            {
                if ( items is not null )
                {
                    for ( var i = 0; i < Count; ++i )
                    {
                        var trackable = (ITrackable)this[ i ];

                        if ( trackable is not null && trackable.IsChanged )
                        {
                            IsChanged = true;
                            break;
                        }
                    }
                }

                SubscribeTrackableItemChanges();
            }
        }

        protected bool IsItemTrackable { get; }

        protected abstract IEqualityComparer<T> Comparer { get; }

        public bool IsChanged
        {
            get => m_changed;
            private set
            {
                if ( m_changed != value )
                {
                    m_changed = value;
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );
                }
            }
        }

        public bool IsInitializing => m_initializing;

        public void BeginInit()
        {
            CheckNotInitializing();

            m_initializing = true;
        }

        public void EndInit()
        {
            if ( !m_initializing )
                throw new InvalidOperationException( "BeginInit must be callled before EndInit." );

            m_initializing = false;
            UpdateChanged();
        }

        public void AcceptChanges()
        {
            CheckNotInitializing();

            if ( IsChanged )
            {
                if ( m_originalCreated )
                {
                    OnDiscardOriginal( out var _ );
                    m_originalCreated = false;
                }

                if ( IsItemTrackable )
                {
                    m_doNotUpdateChanged = true;
                    m_initializing = true;

                    for ( var i = 0; i < Count; ++i )
                    {
                        var trackable = (ITrackable)this[ i ];

                        if ( trackable.IsChanged )
                        {
                            trackable.AcceptChanges();
                        }
                    }

                    m_initializing = false;
                    m_doNotUpdateChanged = false;

                    EnsureOriginal();
                    UpdateChanged();
                }
                else
                {
                    EnsureOriginal();
                    IsChanged = false;
                }
            }
        }

        public void RejectChanges()
        {
            CheckNotInitializing();

            if ( IsChanged )
            {
                m_initializing = true;
                m_doNotUpdateChanged = IsItemTrackable;

                IEnumerable<T>? originalItems = null;

                if ( m_originalCreated )
                {
                    OnDiscardOriginal( out originalItems );
                    m_originalCreated = false;
                }

                if ( IsItemTrackable )
                {
                    foreach ( ITrackable trackable in originalItems ?? this )
                    {
                        if ( trackable.IsChanged )
                        {
                            trackable.RejectChanges();
                        }
                    }
                }

                if ( originalItems is object )
                {
                    Reset( originalItems );
                }

                m_initializing = false;

                if ( IsItemTrackable )
                {
                    m_doNotUpdateChanged = false;
                    UpdateChanged();
                }
                else
                {
                    IsChanged = false;
                }
            }
        }

        /// <summary>
        /// Resets the collection to represent the provided items.
        /// If <paramref name="initialize"/> is true then the collection is considered
        /// unchanged after that if the items themselves are not changed.
        /// </summary>
        public void Reset( IEnumerable<T> items, bool initialize )
        {
            if ( m_initializing || !initialize )
            {
                Reset( items );
            }
            else
            {
                BeginInit();

                if ( m_originalCreated )
                {
                    OnDiscardOriginal( out var _ );
                    m_originalCreated = false;
                }

                Reset( items );
                EndInit();
            }
        }

        public abstract bool OriginalEquals( object? obj );

        public override bool Equals( object? obj )
        {
            if ( ReferenceEquals( this, obj ) )
                return true;

            if ( obj is IEnumerable<T> other )
            {
                var count = other switch
                {
                    ICollection<T> x => x.Count,
                    IReadOnlyCollection<T> x => x.Count,
                    _ => -1
                };

                if ( count != -1 && count != Count )
                    goto NOT_EQUAL;

                var comparer = Comparer;
                var enumerator = GetEnumerator();

                foreach ( var item in other )
                {
                    if ( !enumerator.MoveNext() )
                        goto NOT_EQUAL;

                    if ( !comparer.Equals( item, enumerator.Current ) )
                        goto NOT_EQUAL;
                }

                return !enumerator.MoveNext();
            }

        NOT_EQUAL:
            return false;
        }

        public override int GetHashCode() => base.GetHashCode();

        public abstract IEnumerable<TrackableListChangedItem<T>> GetChangedItems();

        IEnumerable<TrackableListChangedItem> ITrackableList.GetChangedItems()
        {
            foreach ( var changedItem in GetChangedItems() )
            {
                yield return new TrackableListChangedItem( changedItem.Type, changedItem.Item, changedItem.OriginalItem );
            }
        }

        bool ITrackable.IsPropertyChanged( string propertyName ) => false;

        bool ITrackable.IsPropertyTracked( string propertyName ) => false;

        protected abstract bool TryFindOriginalTrackableItem( T current, [MaybeNullWhen( returnValue: false )] out T original );

        protected abstract void OnCreateOriginal();

        protected abstract void OnRemoveOriginalItem( T item );

        protected abstract void OnInsertOriginalItem( T item, int itemIndex );

        protected abstract void OnDiscardOriginal( out IEnumerable<T>? originalItems );

        protected override void OnCollectionChanged()
        {
            if ( !m_initializing )
            {
                UpdateChanged();
            }

            base.OnCollectionChanged();
        }

        protected override void OnInsert( T item, int index )
        {
            if ( IsInitializing )
            {
                if ( m_originalCreated )
                {
                    OnInsertOriginalItem( item, index );
                }
            }
            else
            {
                EnsureOriginal();
            }

            base.OnInsert( item, index );
        }

        protected override void OnRemove( T item, int index )
        {
            if ( IsInitializing )
            {
                if ( m_originalCreated )
                {
                    OnRemoveOriginalItem( item );
                }
            }
            else
            {
                EnsureOriginal();
            }

            base.OnRemove( item, index );
        }

        protected override void OnClear()
        {
            EnsureOriginal();

            base.OnClear();
        }

        protected override void OnReset( List<T> items )
        {
            EnsureOriginal();

            base.OnReset( items );
        }

        protected override void OnMove( T item, int oldIndex, int newIndex )
        {
            EnsureOriginal();

            base.OnMove( item, oldIndex, newIndex );
        }

        protected override void OnReplace( T oldItem, T newItem, int index )
        {
            EnsureOriginal();

            base.OnReplace( oldItem, newItem, index );
        }

        protected void UpdateChanged()
        {
            if ( m_doNotUpdateChanged )
            {
                // This is needed to avoid needlessly checking for changed when we are enumerating items
                // and causing change notifications en masse.
                return;
            }

            IsChanged = !OriginalEquals( this );
        }

        private void SubscribeTrackableItemChanges()
        {
            ItemsChanges.Where( x => x.PropertyChangedEventArgs is TrackablePropertyChangedEventArgs || x.PropertyChangedEventArgs == SharedPropertyChangedEventArgs.IsChanged ).Subscribe( x =>
            {
                if ( TryFindOriginalTrackableItem( current: x.Source, out var original ) )
                {
                    if ( ( (ITrackable)original ).OriginalEquals( x.Source ) )
                    {
                        UpdateChanged();
                    }
                    else
                    {
                        IsChanged = true;
                    }
                }
                else
                {
                    IsChanged = true;
                }
            } );
        }

        protected void EnsureOriginal()
        {
            if ( !m_originalCreated && !m_initializing )
            {
                OnCreateOriginal();
                m_originalCreated = true;
            }
        }

        private void CheckNotInitializing()
        {
            if ( m_initializing )
                throw new InvalidOperationException( "The operation is not valid because the object is initializing." );
        }

        private bool m_doNotUpdateChanged;

        private bool m_changed;
        private bool m_initializing;

        private bool m_originalCreated;
    }
}
