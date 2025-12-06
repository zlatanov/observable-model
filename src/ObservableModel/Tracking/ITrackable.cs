using System.ComponentModel;

namespace ObservableModel
{
    public interface ITrackable : IChangeTracking, INotifyPropertyChanged
    {
        bool IsInitializing { get; }

        bool OriginalEquals( object? obj );

        void RejectChanges();

        bool IsPropertyChanged( string propertyName );

        bool IsPropertyTracked( string propertyName );
    }
}
