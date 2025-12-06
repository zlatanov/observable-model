namespace ObservableModel
{
    public readonly struct TrackablePropertyChange
    {
        internal TrackablePropertyChange( string propertyName, object? originalValue, object? value )
        {
            PropertyName = propertyName;
            OriginalValue = originalValue;
            Value = value;
        }

        public string PropertyName { get; }

        public object? OriginalValue { get; }

        public object? Value { get; }

        public override string ToString() => $"{PropertyName}: {OriginalValue} -> {Value}";
    }
}
