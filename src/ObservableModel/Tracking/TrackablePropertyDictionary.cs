using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ObservableModel
{
    [DebuggerNonUserCode]
    internal sealed class TrackablePropertyDictionary
    {
        public int Count => m_properties.Count;

        public void Add( ITrackableProperty property ) => m_properties.Add( property.Name, property );

        public ITrackableProperty Get( string propertyName )
        {
            if ( m_properties.TryGetValue( propertyName, out var property ) )
                return property;

            throw new ArgumentException( $"Trackable property with the name '{propertyName}' doesn't exist.", nameof( propertyName ) );
        }

        public bool Contains( string propertyName ) => m_properties.ContainsKey( propertyName );

        public ReadOnlySpan<ITrackableProperty>.Enumerator GetEnumerator()
        {
            var array = m_array;

            if ( m_array is null )
            {
                array = new ITrackableProperty[ m_properties.Count ];
                m_properties.Values.CopyTo( array, index: 0 );

                m_array = array;
            }

            return new ReadOnlySpan<ITrackableProperty>( array ).GetEnumerator();
        }

        private readonly Dictionary<string, ITrackableProperty> m_properties = new( StringComparer.Ordinal );
        private ITrackableProperty[]? m_array;
    }
}
