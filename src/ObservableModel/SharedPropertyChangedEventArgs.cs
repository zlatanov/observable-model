using System.Collections.Specialized;
using System.ComponentModel;

namespace ObservableModel
{
    public static class SharedPropertyChangedEventArgs
    {
        public static readonly PropertyChangedEventArgs Value = new( nameof( Value ) );
        public static readonly PropertyChangedEventArgs Indexer = new( "Item[]" );
        public static readonly PropertyChangedEventArgs Count = new( nameof( Count ) );
        public static readonly PropertyChangedEventArgs First = new( nameof( First ) );
        public static readonly PropertyChangedEventArgs Last = new( nameof( Last ) );
        public static readonly PropertyChangedEventArgs IsChanged = new( nameof( IsChanged ) );
        public static readonly PropertyChangedEventArgs IsEmpty = new( nameof( IsEmpty ) );

        public static readonly NotifyCollectionChangedEventArgs CollectionReset = new( NotifyCollectionChangedAction.Reset );
    }
}
