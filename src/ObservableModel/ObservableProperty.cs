using System;
using System.ComponentModel;

namespace ObservableModel
{
    public sealed class ObservableProperty<T> : INotifyPropertyChanged, IDisposable
    {
        public ObservableProperty( IObservable<T> source )
        {
            Changed = source.DistinctUntilChanged();

            m_value = default!;
            m_subscription = source.Subscribe( x => Value = x );
        }


        public ObservableProperty( IObservable<T> source, T initialValue )
        {
            Changed = source.DistinctUntilChanged();

            m_value = initialValue;
            m_subscription = source.Subscribe( x => Value = x );
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        /// <summary>
        /// Gets the value.
        /// </summary>
        public T Value
        {
            get => m_value;
            private set
            {
                m_value = value;
                PropertyChanged?.Raise( this, SharedPropertyChangedEventArgs.Value );
            }
        }


        /// <summary>
        /// Gets the value changed observable.
        /// </summary>
        public IObservable<T> Changed { get; }


        /// <summary>
        /// Unsubscribes the derived property from the property source.
        /// </summary>
        public void Dispose() => m_subscription.Dispose();


        public override string? ToString() => Value is object ? Value.ToString() : String.Empty;


        private T m_value;
        private readonly IDisposable m_subscription;
    }
}
