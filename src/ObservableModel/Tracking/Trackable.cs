using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace ObservableModel
{
    public abstract class Trackable : ObservableObject, ITrackable, ISupportInitialize
    {
        internal static readonly MethodInfo BeginInitMethod = typeof( Trackable ).GetMethod( nameof( BeginInit ), BindingFlags.Public | BindingFlags.Instance )!;
        internal static readonly MethodInfo EndInitMethod = typeof( Trackable ).GetMethod( nameof( EndInit ), BindingFlags.Public | BindingFlags.Instance )!;


        /// <summary>
        /// Returns whether this instance is actively being tracked for changes.<br />
        /// The only way to actively track an object is to create it from <seealso cref="Trackable{T}.Create"/>.
        /// </summary>
        public static bool IsTracked( Trackable instance ) => instance.m_trackedProperties is not null;


        public bool IsChanged => m_changedProperties?.Count > 0;


        public bool IsInitializing => m_initializing > 0;


        public void BeginInit()
        {
            if ( !IsInitializing && IsChanged )
                throw new InvalidOperationException( "The object cannot begin initializing because it has pending changes." );

            checked
            {
                // Nobody should ever be doing so many BeginInit calls to the same object
                ++m_initializing;
            }
        }


        public void EndInit()
        {
            if ( !IsInitializing )
                throw new InvalidOperationException( "BeginInit must be callled before EndInit." );

            --m_initializing;
        }


        public void AcceptChanges()
        {
            if ( IsChanged )
            {
                OnAcceptChanges();

                if ( m_changedProperties!.Count == 0 )
                {
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );
                }
            }
        }


        public void RejectChanges()
        {
            if ( IsChanged )
            {
                OnRejectChanges();

                if ( m_changedProperties!.Count == 0 )
                {
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );
                }
            }
        }


        public void RejectChanges( string propertyName ) => GetTrackedProperty( propertyName ).RejectChanges( this );


        public bool IsPropertyTracked( string propertyName ) => m_trackedProperties?.Contains( propertyName ) == true;


        public bool IsPropertyChanged( string propertyName ) => m_changedProperties?.Contains( propertyName ) == true;


        public bool IsPropertyChanged( string propertyName, Trackable original )
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            return m_trackedProperties.Get( propertyName ).IsChanged( this, original );
        }


        public bool OriginalEquals( object? obj )
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            if ( obj is null || obj.GetType() != GetType() )
                return false;

            var other = (Trackable)obj;

            foreach ( var property in m_trackedProperties )
            {
                if ( !property.OriginalEquals( original: this, current: other ) )
                    return false;
            }

            return true;
        }


        public TrackablePropertyChange[] GetChanges( Trackable original )
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            var changes = new TrackablePropertyChange[ m_trackedProperties.Count ];
            var changeCount = 0;

            foreach ( var property in m_trackedProperties )
            {
                if ( !property.OriginalEquals( original, current: this ) )
                {
                    changes[ changeCount ] = new TrackablePropertyChange( property.Name, property.GetOriginalValue( original ), property.GetCurrentValue( this ) );
                    changeCount += 1;
                }
            }

            if ( changeCount == 0 )
                return [];

            Array.Resize( ref changes, changeCount );

            return changes;
        }


        public TrackablePropertyChange[] GetChanges()
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            if ( m_changedProperties is null || m_changedProperties.Count == 0 )
                return [];

            var changes = new TrackablePropertyChange[ m_changedProperties.Count ];
            var changeIndex = 0;

            foreach ( var propertyName in m_changedProperties )
            {
                var property = m_trackedProperties.Get( propertyName );

                changes[ changeIndex++ ] = new TrackablePropertyChange( property.Name, property.GetOriginalValue( this ), property.GetCurrentValue( this ) );
            }

            return changes;
        }


        public T GetOriginalValue<T>( string propertyName ) => ( (ICovariantTrackableProperty<T>)GetTrackedProperty( propertyName ) ).GetOriginalValue( this );


        public object? GetOriginalValue( string propertyName ) => GetTrackedProperty( propertyName ).GetOriginalValue( this );


        private ITrackableProperty GetTrackedProperty( string propertyName )
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            return m_trackedProperties.Get( propertyName );
        }


        /// <summary>
        /// Resets the current and original value for the given property to the given value.
        /// </summary>
        public void ResetValue<T>( string propertyName, T value )
        {
            var property = (TrackableProperty<T>)GetTrackedProperty( propertyName );

            property.SetOriginalValue( this, value );
            property.SetValue( this, value );
        }


        public void SetOriginalValue<T>( string propertyName, T value )
        {
            var property = (TrackableProperty<T>)GetTrackedProperty( propertyName );

            property.SetOriginalValue( this, value );

            if ( !IsPropertyChanged( propertyName ) )
            {
                property.SetCurrentValue( this, value );
            }
            else
            {
                var current = property.GetCurrentValue( this );

                if ( property.ValueEquals( current, value ) )
                {
                    UnmarkChanged( property.Name, raiseEvent: true );
                }
                else
                {
                    MarkChanged( property.Name, raiseEvent: true );
                }
            }
        }


        protected virtual void OnAcceptChanges()
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            foreach ( var property in m_trackedProperties )
            {
                property.AcceptChanges( this );
            }
        }


        protected virtual void OnRejectChanges()
        {
            if ( m_trackedProperties is null )
                throw InvalidOperationException();

            foreach ( var property in m_trackedProperties )
            {
                property.RejectChanges( this );
            }
        }


        protected internal virtual void OnTrackablePropertyChanged( string propertyName )
        {
        }


        internal void MarkChanged( string propertyName, bool raiseEvent = false )
        {
            var properties = m_changedProperties ??= new HashSet<string>( StringComparer.Ordinal );

            if ( properties.Add( propertyName ) && raiseEvent )
            {
                if ( m_changedProperties.Count == 1 )
                {
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );
                }
            }
        }


        internal bool UnmarkChanged( string propertyName, bool raiseEvent = false )
        {
            var unmarked = m_changedProperties?.Remove( propertyName ) == true;

            if ( unmarked && raiseEvent )
            {
                if ( m_changedProperties!.Count == 0 )
                {
                    RaisePropertyChanged( SharedPropertyChangedEventArgs.IsChanged );
                }
            }

            return unmarked;
        }


        internal void Attach( ITrackable value, ITrackableProperty property )
        {
            var handler = new PropertyChangedEventHandler( ( sender, e ) => OnTrackablePropertyChanged( sender, e, property ) );

            m_changeHandlers ??= new Dictionary<string, PropertyChangedEventHandler>( StringComparer.Ordinal );
            m_changeHandlers.Add( property.Name, handler );

            value.PropertyChanged += handler;
        }


        internal void Detach( ITrackable value, ITrackableProperty property )
        {
            if ( m_changeHandlers is not null && m_changeHandlers.Remove( property.Name, out var handler ) )
            {
                value.PropertyChanged -= handler;
            }
        }


        private void OnTrackablePropertyChanged( object? sender, PropertyChangedEventArgs e, ITrackableProperty property )
        {
            if ( e == SharedPropertyChangedEventArgs.IsChanged )
            {
                var current = (ITrackable)sender!;

                if ( current.IsChanged )
                {
                    MarkChanged( property.Name, raiseEvent: true );
                }
                else
                {
                    var original = ( (ICovariantTrackableProperty<ITrackable>)property ).GetOriginalValue( this );

                    if ( original is object && original.OriginalEquals( current ) )
                    {
                        UnmarkChanged( property.Name, raiseEvent: true );
                    }
                }
            }
        }


        private static InvalidOperationException InvalidOperationException() => new( "The object is not tracked. Use Tracking<T>.Create() to create a trackable instance." );


        private byte m_initializing;

        private HashSet<string>? m_changedProperties;
        private Dictionary<string, PropertyChangedEventHandler>? m_changeHandlers;

        private protected TrackablePropertyDictionary? m_trackedProperties;
    }
}
