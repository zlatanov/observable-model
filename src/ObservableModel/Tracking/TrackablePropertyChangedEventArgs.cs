using System.ComponentModel;

namespace ObservableModel
{
    public sealed class TrackablePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public TrackablePropertyChangedEventArgs( string propertyName ) : base( propertyName )
        {
        }
    }
}
