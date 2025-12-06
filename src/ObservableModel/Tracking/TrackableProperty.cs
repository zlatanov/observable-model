using System;
using System.Collections.Generic;

namespace ObservableModel
{
    internal abstract class TrackableProperty<T> : ITrackableProperty, ICovariantTrackableProperty<T>
    {
        protected TrackableProperty( string name, bool referenceOnly, bool readOnly )
        {
            Name = name;
            Type = typeof( T );

            m_referenceOnly = referenceOnly;
            m_readOnly = readOnly;
            m_isClass = typeof( T ).IsClass;

            if ( !referenceOnly )
            {
                m_trackable = typeof( ITrackable ).IsAssignableFrom( typeof( T ) );
                m_comparer = EqualityComparer<T>.Default;
            }
        }


        public string Name { get; }


        public Type Type { get; }


        public bool ValueEquals( T x, T y )
        {
            if ( m_referenceOnly )
                return ReferenceEquals( x, y );

            // If the compared values are references, check if they are the same
            if ( m_isClass && ReferenceEquals( x, y ) )
                return true;

            return m_comparer!.Equals( x, y );
        }


        public bool OriginalEquals( Trackable original, Trackable current )
        {
            var originalValue = GetOriginalValue( original );
            var currentValue = GetCurrentValue( current );

            if ( m_trackable )
            {
                if ( originalValue is object && currentValue is object )
                    return ( (ITrackable)originalValue ).OriginalEquals( currentValue );

                return originalValue is null && currentValue is null;
            }

            return ValueEquals( originalValue, currentValue );
        }


        public void SetValue( Trackable owner, T value )
        {
            var changed = owner.IsChanged;
            var propertyChanged = m_trackable ? SetTrackableValue( owner, value ) : SetNonTrackableValue( owner, value );

            if ( propertyChanged )
                SetCurrentValue( owner, value );

            if ( changed != owner.IsChanged )
                owner.RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );

            if ( propertyChanged )
            {
                if ( !owner.IsInitializing )
                    owner.OnTrackablePropertyChanged( Name );

                RaisePropertyChanged( owner );
            }
        }


        public bool IsChanged( Trackable owner, Trackable original )
        {
            var originalValue = GetOriginalValue( original );
            var currentValue = GetCurrentValue( owner );

            return !ValueEquals( currentValue, originalValue );
        }


        /// <summary>
        /// Returns whether the value has really changed.
        /// </summary>
        private bool SetNonTrackableValue( Trackable owner, T value )
        {
            var initializing = owner.IsInitializing;
            var current = GetCurrentValue( owner );

            if ( initializing )
            {
                SetOriginalValue( owner, value );
            }

            if ( ValueEquals( current, value ) )
            {
                // If the value is reference type even though the values
                // are structurally equal to each other we should return that
                // the value has changed if they are not the same reference.
                if ( m_isClass )
                    return !ReferenceEquals( current, value );

                return false;
            }

            if ( !initializing )
            {
                var original = GetOriginalValue( owner );

                if ( ValueEquals( original, value ) )
                {
                    // The value is the same as the original value and since
                    // we know that the value has changed, it in turn means
                    // that the property should no longer be considered changed.
                    owner.UnmarkChanged( Name );
                }
                else
                {
                    owner.MarkChanged( Name );
                }
            }

            return true;
        }


        internal bool SetTrackableValue( Trackable owner, T value )
        {
            var current = GetCurrentValue( owner );

            if ( current is object v )
                owner.Detach( (ITrackable)v, this );

            var initializing = owner.IsInitializing;
            var different = false;

            if ( initializing )
            {
                SetOriginalValue( owner, value );
            }
            else
            {
                var original = GetOriginalValue( owner );
                different = original is ITrackable x ? !x.OriginalEquals( value ) : !ValueEquals( original, value );
            }

            if ( value is object )
            {
                different |= ( (ITrackable)value ).IsChanged;
                owner.Attach( (ITrackable)value, this );
            }

            if ( different )
            {
                owner.MarkChanged( Name );
            }
            else if ( !initializing )
            {
                owner.UnmarkChanged( Name );
            }

            return !ReferenceEquals( current, value );
        }


        public void AcceptChanges( Trackable owner )
        {
            if ( owner.UnmarkChanged( Name ) )
            {
                var currentValue = GetCurrentValue( owner );

                if ( !m_readOnly )
                {
                    SetOriginalValue( owner, currentValue );
                }

                if ( m_trackable && currentValue is ITrackable trackable && trackable.IsChanged )
                {
                    trackable.AcceptChanges();
                }
            }
        }


        public void RejectChanges( Trackable owner )
        {
            if ( owner.UnmarkChanged( Name ) )
            {
                var originalValue = GetOriginalValue( owner );

                if ( m_trackable && originalValue is ITrackable trackable && trackable.IsChanged )
                {
                    trackable.RejectChanges();
                }

                if ( !m_readOnly )
                {
                    SetCurrentValue( owner, originalValue );
                    RaisePropertyChanged( owner );
                }
            }
        }


        public abstract T GetOriginalValue( Trackable owner );


        object? ITrackableProperty.GetOriginalValue( Trackable owner )
        {
            return GetOriginalValue( owner );
        }


        object? ITrackableProperty.GetCurrentValue( Trackable owner )
        {
            return GetCurrentValue( owner );
        }


        public abstract void SetOriginalValue( Trackable owner, T value );


        public abstract T GetCurrentValue( Trackable owner );


        public virtual void SetCurrentValue( Trackable owner, T value ) => throw new InvalidOperationException( $"The property {Name} is readonly and cannot be changed." );


        internal void RaisePropertyChanged( Trackable owner )
            => owner.RaisePropertyChanged( m_propertyChangedEventArgs ??= new TrackablePropertyChangedEventArgs( Name ) );


        private readonly bool m_referenceOnly;
        private readonly bool m_readOnly;
        private readonly bool m_trackable;
        private readonly bool m_isClass;

        private readonly EqualityComparer<T>? m_comparer;
        private TrackablePropertyChangedEventArgs? m_propertyChangedEventArgs;
    }
}
