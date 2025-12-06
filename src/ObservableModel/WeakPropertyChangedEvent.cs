using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime;

namespace ObservableModel
{
    public sealed class WeakPropertyChangedEvent : List<DependentHandle>
    {
        public static void Add( ref WeakPropertyChangedEvent? list, PropertyChangedEventHandler? handler )
        {
            if ( handler is null )
                return;

            list ??= [];
            list.Add( new DependentHandle( handler.Target ?? list, handler ) );
        }

        public static void Remove( ref WeakPropertyChangedEvent? list, PropertyChangedEventHandler? handler )
        {
            if ( handler is null || list is null )
                return;

            for ( var index = 0; index < list.Count; )
            {
                var dependentHandle = list[ index ];
                var (target, dependenent) = dependentHandle.TargetAndDependent;

                if ( target is not null )
                {
                    if ( (PropertyChangedEventHandler?)dependenent == handler )
                    {
                        dependentHandle.Dispose();
                        list.RemoveAt( index );
                        break;
                    }

                    index += 1;
                }
                else
                {
                    dependentHandle.Dispose();
                    list.RemoveAt( index );
                }
            }

            if ( list.Count == 0 )
                list = null;
        }

        public void Invoke( object sender, string? propertyName )
        {
            if ( Count > 0 )
            {
                Invoke( sender, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        public void Invoke( object sender, PropertyChangedEventArgs e )
        {
            for ( var index = 0; index < Count; ++index )
            {
                var dependentHandle = this[ index ];
                var (target, dependenent) = dependentHandle.TargetAndDependent;

                if ( dependenent is PropertyChangedEventHandler handler )
                {
                    handler( sender, e );
                    index += 1;
                }
                else
                {
                    dependentHandle.Dispose();
                    RemoveAt( index );
                }
            }
        }
    }
}
