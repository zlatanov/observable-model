using System;
using System.ComponentModel;

namespace ObservableModel
{
    public interface IObservableObject : INotifyPropertyChanged
    {
        IObservable<ObservablePropertyChange> PropertyChanges { get; }

        IDisposable DeferPropertyChanges();
    }
}
