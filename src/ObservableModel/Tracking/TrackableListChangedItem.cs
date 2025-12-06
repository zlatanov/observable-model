using System;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    public readonly struct TrackableListChangedItem<T> where T : notnull
    {
        public TrackableListChangedItem( TrackableListChangeType type, T item, T originalItem )
        {
            Type = type;
            Item = item;
            OriginalItem = originalItem;
        }


        public TrackableListChangeType Type { get; }
        public T Item { get; }

        [MaybeNull]
        public T OriginalItem { get; }


        public bool IsPropertyChanged( string propertyName )
        {
            if ( Item is not Trackable item  )
                throw new InvalidOperationException( "This method is only valid for trackable items." );

            if ( OriginalItem is not Trackable originalItem  )
                return item.IsPropertyChanged( propertyName );

            return item.IsPropertyChanged( propertyName, originalItem );
        }
    }


    public readonly struct TrackableListChangedItem
    {
        public static TrackableListChangedItem<T> Add<T>( T item ) where T : notnull => new( TrackableListChangeType.Add, item, default! );
        public static TrackableListChangedItem Add( object item ) => new( TrackableListChangeType.Add, item, null );

        public static TrackableListChangedItem<T> Remove<T>( T item ) where T : notnull => new( TrackableListChangeType.Remove, item, item );
        public static TrackableListChangedItem Remove( object item ) => new( TrackableListChangeType.Remove, item, item );

        public static TrackableListChangedItem<T> Change<T>( T item, T originalItem ) where T : notnull => new( TrackableListChangeType.Change, item, originalItem );
        public static TrackableListChangedItem Change( object item, object originalItem ) => new( TrackableListChangeType.Change, item, originalItem );


        public TrackableListChangedItem( TrackableListChangeType type, object item, object? originalItem )
        {
            Type = type;
            Item = item;
            OriginalItem = originalItem;
        }


        public TrackableListChangeType Type { get; }
        public object Item { get; }
        public object? OriginalItem { get; }


        public bool IsPropertyChanged( string propertyName )
        {
            if ( Item is not Trackable item  )
                throw new InvalidOperationException( "This method is only valid for trackable items." );

            if ( OriginalItem is not Trackable originalItem  )
                return item.IsPropertyChanged( propertyName );

            return item.IsPropertyChanged( propertyName, originalItem );
        }
    }
}
