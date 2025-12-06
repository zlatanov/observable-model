using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ObservableModel.Subjects;

namespace ObservableModel
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        internal static readonly MethodInfo RaisePropertyChangedMethod = typeof( ObservableObject ).GetMethod( nameof( RaisePropertyChanged ), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, [ typeof( string ), typeof( PropertyChangedEventArgs ).MakeByRefType() ], modifiers: null )!;


        public event PropertyChangedEventHandler? PropertyChanged;


        public IObservable<ObservablePropertyChange> PropertyChanges => m_propertyChanges ??= new Subject<ObservablePropertyChange>();


        /// <summary>
        /// Defers <seealso cref="PropertyChanged" /> event and <seealso cref="PropertyChanges" /> observable.
        /// </summary>
        public IDisposable DeferPropertyChanges()
        {
            if ( m_deferredPropertyChanges is not null )
                throw new InvalidOperationException( "Property changes are already being deferred." );

            m_deferredPropertyChanges = [];

            return new Disposable( RestorePropertyChanges );
        }


        protected void RaisePropertyChanged( [CallerMemberName] string? propertyName = null )
        {
            if ( PropertyChanged is not null || m_propertyChanges is not null )
            {
                RaisePropertyChangedCore( new PropertyChangedEventArgs( propertyName ?? String.Empty ) );
            }
        }


        public void RaisePropertyChanged( PropertyChangedEventArgs args )
        {
            if ( PropertyChanged is not null || m_propertyChanges is not null )
            {
                RaisePropertyChangedCore( args );
            }
        }


        protected void RaisePropertyChanged( string propertyName, ref PropertyChangedEventArgs? args )
        {
            if ( PropertyChanged is not null || m_propertyChanges is not null )
            {
                RaisePropertyChangedCore( Volatile.Read( ref args ) ?? Initialize( propertyName, ref args ) );
            }

            static PropertyChangedEventArgs Initialize( string propertyName, ref PropertyChangedEventArgs? target )
            {
                var value = new PropertyChangedEventArgs( propertyName );

                return Interlocked.CompareExchange( ref target, value, comparand: null ) ?? value;
            }
        }


        private void RaisePropertyChangedCore( PropertyChangedEventArgs args )
        {
            if ( m_deferredPropertyChanges is not null )
            {
                // Changes are suspended. Add the new change only if it doesn't already exist.
                foreach ( var pendingChange in m_deferredPropertyChanges )
                {
                    if ( StringComparer.Ordinal.Equals( pendingChange.PropertyName, args.PropertyName ) )
                    {
                        return;
                    }
                }

                m_deferredPropertyChanges.Add( args );

                return;
            }

            PropertyChanged?.Invoke( this, args );
            m_propertyChanges?.OnNext( new ObservablePropertyChange( this, args ) );

            foreach ( var reference in args.GetRelatedEventArgs( GetType() ) )
            {
                RaisePropertyChangedCore( reference );
            }
        }


        protected bool SetValue<T>( ref T oldValue, T newValue, [CallerMemberName] string? propertyName = null )
        {
            if ( Equality.IsDifferent( oldValue, newValue ) )
            {
                oldValue = newValue;
                RaisePropertyChanged( propertyName );

                return true;
            }

            return false;
        }


        protected bool SetValue<T>( ref T oldValue, T newValue, string propertyName, ref PropertyChangedEventArgs? args )
        {
            if ( Equality.IsDifferent( oldValue, newValue ) )
            {
                oldValue = newValue;
                RaisePropertyChanged( propertyName, ref args );

                return true;
            }

            return false;
        }


        protected bool SetValue<T>( ref T oldValue, T newValue, PropertyChangedEventArgs args )
        {
            if ( Equality.IsDifferent( oldValue, newValue ) )
            {
                oldValue = newValue;
                RaisePropertyChanged( args );

                return true;
            }

            return false;
        }


        private void RestorePropertyChanges()
        {
            Debug.Assert( m_deferredPropertyChanges is not null );

            var propertyChanges = m_deferredPropertyChanges;
            m_deferredPropertyChanges = null;

            foreach ( var property in propertyChanges )
            {
                RaisePropertyChanged( property );
            }
        }


        private Subject<ObservablePropertyChange>? m_propertyChanges;
        private List<PropertyChangedEventArgs>? m_deferredPropertyChanges;
    }
}
