using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ObservableModel
{
    internal static class ReflectionHelper
    {
        public static PropertyInfo? GetProperty( Type type, string propertyName )
        {
            var properties = s_metadata.GetOrAdd( type, s_metadataFactory );

            foreach ( var property in properties )
            {
                if ( property.Name == propertyName )
                {
                    return property;
                }
            }

            return null;
        }


        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> s_metadata = new();
        private static readonly Func<Type, PropertyInfo[]> s_metadataFactory = x => x.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
    }
}
