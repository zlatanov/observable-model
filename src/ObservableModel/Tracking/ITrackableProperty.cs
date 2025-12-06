using System;

namespace ObservableModel
{
    internal interface ITrackableProperty
    {
        string Name { get; }

        Type Type { get; }

        void AcceptChanges( Trackable owner );

        void RejectChanges( Trackable owner );

        bool OriginalEquals( Trackable original, Trackable current );

        bool IsChanged( Trackable owner, Trackable original );

        object? GetOriginalValue( Trackable owner );

        object? GetCurrentValue( Trackable owner );
    }


    internal interface ICovariantTrackableProperty<out T> : ITrackableProperty
    {
        new T GetOriginalValue( Trackable owner );
    }
}
