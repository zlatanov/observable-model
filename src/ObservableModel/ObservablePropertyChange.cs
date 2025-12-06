using System.ComponentModel;

namespace ObservableModel
{
    public readonly struct ObservablePropertyChange<T>
    {
        internal ObservablePropertyChange( T source, PropertyChangedEventArgs args )
        {
            Source = source;
            PropertyChangedEventArgs = args;
        }


        public T Source { get; }
        public string? PropertyName => PropertyChangedEventArgs.PropertyName;
        public PropertyChangedEventArgs PropertyChangedEventArgs { get; }
    }


    public readonly struct ObservablePropertyChange
    {
        internal ObservablePropertyChange( object source, PropertyChangedEventArgs args )
        {
            Source = source;
            PropertyChangedEventArgs = args;
        }


        public object Source { get; }
        public string? PropertyName => PropertyChangedEventArgs.PropertyName;
        public PropertyChangedEventArgs PropertyChangedEventArgs { get; }
    }
}
