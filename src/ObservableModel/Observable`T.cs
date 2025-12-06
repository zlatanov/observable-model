using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace ObservableModel
{
    public static class Observable<T> where T : ObservableObject
    {
        private static readonly ExceptionDispatchInfo? _exception;
        private static readonly Type? _type;


        public static T Create( Action<T>? builder = null )
        {
            _exception?.Throw();

            var observable = (T)ObjectConstructor.Create( _type! );

            if ( builder is not null )
            {
                builder( observable );
            }

            return observable;
        }


        public static T Create<TArgs>( in TArgs args, Action<T>? builder = null )
        {
            _exception?.Throw();

            var observable = (T)ObjectConstructor.Create( _type!, args );

            if ( builder is not null )
            {
                builder( observable );
            }

            return observable;
        }


        static Observable()
        {
            var observableType = typeof( T );
            var properties = observableType.GetProperties( BindingFlags.Instance | BindingFlags.Public )
                                           .Where( x => x.GetCustomAttribute<ObservablePropertyAttribute>() is not null )
                                           .ToList();

            lock ( Dynamic.Lock )
            {
                try
                {
                    var type = Dynamic.DefineType( $"Observable<{observableType.Name}>", TypeAttributes.Public | TypeAttributes.Sealed, parent: observableType );

                    DefineContructors( type );

                    foreach ( var property in properties )
                    {
                        OverrideObservableProperty( type, property );
                    }

                    _type = type.CreateType()!;
                }
                catch ( Exception ex )
                {
                    _exception = ExceptionDispatchInfo.Capture( ex );
                }
            }
        }


        private static void DefineContructors( TypeBuilder type )
        {
            var constructors = typeof( T ).GetConstructors( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );

            if ( constructors.Length == 0 )
            {
                var ctor = type.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes )
                               .GetILGenerator();

                ctor.Emit( OpCodes.Ret );
            }
            else
            {
                foreach ( var baseCtor in constructors )
                {
                    var parameters = baseCtor.GetParameters();
                    var ctor = type.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, parameters.Select( x => x.ParameterType ).ToArray() )
                                   .GetILGenerator();

                    ctor.Emit( OpCodes.Ldarg_0 );

                    for ( var i = 0; i < parameters.Length; ++i )
                    {
                        switch ( i )
                        {
                            case 0: ctor.Emit( OpCodes.Ldarg_1 ); break;
                            case 1: ctor.Emit( OpCodes.Ldarg_2 ); break;
                            case 2: ctor.Emit( OpCodes.Ldarg_3 ); break;
                            default: ctor.Emit( OpCodes.Ldarg_S, checked((byte)( i + 1 )) ); break;
                        }
                    }

                    ctor.Emit( OpCodes.Call, baseCtor );
                    ctor.Emit( OpCodes.Ret );
                }
            }
        }


        private static void OverrideObservableProperty( TypeBuilder type, PropertyInfo property )
        {
            var baseSetMethod = property.GetSetMethod( nonPublic: true ) ?? throw new InvalidOperationException( $"The property {property.Name} doesn't have a setter." );
            var baseGetMethod = property.GetGetMethod() ?? throw new InvalidOperationException( $"The property {property.Name} doesn't have a getter." );

            var propertyChangedEventArgs = type.DefineField( property.Name + "PropertyChangedEventArgs", typeof( PropertyChangedEventArgs ), FieldAttributes.Static | FieldAttributes.Private );
            var method = type.DefineMethod( name: $"Set{property.Name}",
                                                    attributes: MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
                                                    returnType: null,
                                                    parameterTypes: [ property.PropertyType ] );

            method.DefineParameter( 1, ParameterAttributes.None, "value" );

            // if ( Equality.IsDifferent( base.Property, value ) )
            // {
            //      base.Property = value;
            //      RaisePropertyChanged( "Property", ref _propertyChangedEventArgs );
            // }
            var x = method.GetILGenerator();
            var IL_RETURN = x.DefineLabel();

            // if ( Equality.IsDifferent( base.Property, value ) )
            x.Emit( OpCodes.Ldarg_0 );
            x.Emit( OpCodes.Call, baseGetMethod );
            x.Emit( OpCodes.Ldarg_1 );
            x.Emit( OpCodes.Call, typeof( Equality ).GetMethod( "IsDifferent" )!.MakeGenericMethod( property.PropertyType ) );
            x.Emit( OpCodes.Brfalse_S, IL_RETURN );

            // base.Property = value;
            x.Emit( OpCodes.Ldarg_0 );
            x.Emit( OpCodes.Ldarg_1 );
            x.Emit( OpCodes.Call, baseSetMethod );

            // RaisePropertyChanged( "Property", ref _propertyChangedEventArgs );
            x.Emit( OpCodes.Ldarg_0 );
            x.Emit( OpCodes.Ldstr, property.Name );
            x.Emit( OpCodes.Ldsflda, propertyChangedEventArgs );
            x.Emit( OpCodes.Call, ObservableObject.RaisePropertyChangedMethod );

            x.MarkLabel( IL_RETURN );
            x.Emit( OpCodes.Ret );

            type.DefineMethodOverride( method, baseSetMethod );
        }
    }
}
