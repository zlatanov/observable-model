using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ObservableModel
{
    public static class ObservablePropertyDependencyExtensions
    {
        private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyChangedEventArgs[]> Dependencies = new();
        private static readonly PropertyChangedEventArgsComparer Comparer = new();

        /// <summary>
        /// Returns an array of <see cref="PropertyChangedEventArgs" /> for all properties that depend on the changed property.
        /// </summary>
        public static PropertyChangedEventArgs[] GetRelatedEventArgs( this PropertyChangedEventArgs propertyChangedEventArgs, object? owner )
        {
            if ( owner is null )
            {
                return [];
            }

            return GetRelatedEventArgs( propertyChangedEventArgs, owner.GetType() );
        }

        /// <summary>
        /// Returns an array of <see cref="PropertyChangedEventArgs" /> for all properties that depend on the changed property.
        /// </summary>
        public static PropertyChangedEventArgs[] GetRelatedEventArgs( this PropertyChangedEventArgs propertyChangedEventArgs, Type ownerType )
        {
            if ( String.IsNullOrEmpty( propertyChangedEventArgs.PropertyName ) )
            {
                return [];
            }

            return Dependencies.GetOrAdd( (ownerType, propertyChangedEventArgs.PropertyName),
                static x => CreatePropertyChangedEventArgs( x.Type, x.PropertyName, [] ) );
        }

        private static PropertyChangedEventArgs[] CreatePropertyChangedEventArgs( Type ownerType, string propertyName, HashSet<string> recursionGuard )
        {
            if ( !recursionGuard.Add( propertyName ) )
                return [];

            var dependencies = new HashSet<PropertyChangedEventArgs>( Comparer );

            foreach ( var dependencyProperty in ownerType.GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) )
            {
                Load( dependencyProperty );
            }

            // Since C# 8.0 we can have property implementation in interfaces. In order to support change notification
            // for such properties we must check all implemented interfaces for such properties.
            foreach ( var interfaceType in ownerType.GetInterfaces() )
            {
                foreach ( var dependencyProperty in interfaceType.GetProperties( BindingFlags.Public | BindingFlags.Instance ) )
                {
                    Load( dependencyProperty );
                }
            }

            // If for some reason any of the properties has included the original property as dependency, we will remove it
            // because a property should not and must not depend on itself.
            if ( dependencies.Remove( new PropertyChangedEventArgs( propertyName ) ) )
            {
                Trace.TraceWarning( $"Property {propertyName} in {ownerType} contains recursive [ObservablePropertyDependency] which points back to itself." );
            }

            void Load( PropertyInfo dependencyProperty )
            {
                if ( dependencyProperty.Name is null || dependencyProperty.Name == propertyName )
                    return;

                var dependencyAttribute = dependencyProperty.GetCustomAttribute<ObservablePropertyDependencyAttribute>();

                if ( dependencyAttribute is not null && dependencyAttribute.PropertyNames.Contains( propertyName ) )
                {
                    dependencies.Add( new PropertyChangedEventArgs( dependencyProperty.Name ) );

                    // Add as dependency all properties that are corelated to this property also
                    dependencies.UnionWith( Dependencies.GetOrAdd( (ownerType, dependencyProperty.Name), x => CreatePropertyChangedEventArgs( x.Type, x.PropertyName, recursionGuard ) ) );
                }
            }

            return dependencies.ToArray();
        }

        private sealed class PropertyChangedEventArgsComparer : IEqualityComparer<PropertyChangedEventArgs>
        {
            public bool Equals( PropertyChangedEventArgs? x, PropertyChangedEventArgs? y ) => String.Equals( x?.PropertyName, y?.PropertyName, StringComparison.Ordinal );

            public int GetHashCode( [DisallowNull] PropertyChangedEventArgs obj ) => StringComparer.Ordinal.GetHashCode( obj.PropertyName! );
        }
    }
}
