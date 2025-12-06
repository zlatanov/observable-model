using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace ObservableModel
{
    using static TrackableHelpers;

    public static class Trackable<T> where T : Trackable
    {
        private static readonly ExceptionDispatchInfo? _exception;
        private static readonly Type? _type;


        public static T Create( Action<T>? builder = null )
        {
            _exception?.Throw();

            var trackable = (T)ObjectConstructor.Create( _type! );

            if ( builder is not null )
            {
                trackable.BeginInit();
                builder( trackable );
                trackable.EndInit();
            }

            return trackable;
        }


        public static T Create<TArgs>( TArgs args, Action<T>? builder = null )
        {
            _exception?.Throw();

            var trackable = (T)ObjectConstructor.Create( _type!, args );

            if ( builder is not null )
            {
                trackable.BeginInit();
                builder( trackable );
                trackable.EndInit();
            }

            return trackable;
        }


        static Trackable()
        {
            var trackableType = typeof( T );
            var properties = trackableType.GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic )
                                          .Where( x => x.GetCustomAttribute<ObservablePropertyAttribute>() is not null )
                                          .Select( x => x.GetCustomAttribute<TrackablePropertyAttribute>() is TrackablePropertyAttribute attr
                                                      ? new TrackableProperty( x, attr ) : new Property( x ) ).ToList();
            var trackableProperties = properties.OfType<TrackableProperty>().ToList();

            lock ( Dynamic.Lock )
            {
                try
                {
                    var type = Dynamic.DefineType( $"Trackable<{trackableType.Name}>", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, parent: trackableType );

                    // Adding ITrackableEquatable<T> so we can implement equals for only trackable properties
                    type.AddInterfaceImplementation( typeof( ITrackableEquatable<> ).MakeGenericType( trackableType ) );

                    OverrideConstructors( type, trackableProperties );

                    foreach ( var property in properties )
                    {
                        if ( property is TrackableProperty trackableProperty )
                        {
                            if ( !property.IsReadOnly )
                            {
                                OverrideTrackableProperty( type, trackableProperty );
                            }
                        }
                        else
                        {
                            OverrideObservableProperty( type, property );
                        }
                    }

                    OverrideEquals( type, trackableProperties );

                    _type = type.CreateType()!;

                    foreach ( var property in trackableProperties )
                    {
                        property.TrackablePropertyFieldTypeBuilder.CreateType();
                    }
                }
                catch ( Exception ex )
                {
                    _exception = ExceptionDispatchInfo.Capture( ex );
                }
            }
        }


        private static void DefineTrackablePropertyType( TypeBuilder owner, TrackableProperty property )
        {
            var builder = owner.DefineNestedType( name: property.Name + "TrackableProperty",
                                                  attr: TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout,
                                                  parent: typeof( TrackableProperty<> ).MakeGenericType( property.PropertyType ) );

            Debug.Assert( builder.BaseType is not null );

            property.OriginalField = owner.DefineField( $"_<orig>{property.Name}", property.PropertyType, FieldAttributes.Private );
            property.TrackablePropertyFieldType = builder.BaseType;
            property.TrackablePropertyField = builder.DefineField( "Default", builder, FieldAttributes.Static | FieldAttributes.Public | FieldAttributes.InitOnly );
            property.TrackablePropertyFieldTypeBuilder = builder;

            var constructorTypes = new Type[] { typeof( string ), typeof( bool ), typeof( bool ) };
            var constructor = builder.DefineConstructor( MethodAttributes.Private, CallingConventions.Standard, constructorTypes );

            Generate( constructor, x =>
            {
                x.Emit( OpCodes.Ldarg_0 );
                x.Emit( OpCodes.Ldarg_1 );
                x.Emit( OpCodes.Ldarg_2 );
                x.Emit( OpCodes.Ldarg_3 );
                x.Emit( OpCodes.Call, builder.BaseType.GetConstructor( BindingFlags.NonPublic | BindingFlags.Instance, binder: null, constructorTypes, modifiers: null )! );
                x.Emit( OpCodes.Ret );
            } );

            // Define static constructor to create the the default instance
            Generate( builder.DefineConstructor( MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes ), x =>
            {
                x.Emit( OpCodes.Ldstr, property.Name );
                x.Emit( property.ReferenceOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0 );
                x.Emit( property.IsReadOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0 );
                x.Emit( OpCodes.Newobj, constructor );
                x.Emit( OpCodes.Stsfld, property.TrackablePropertyField );
                x.Emit( OpCodes.Ret );
            } );

            {   // T GetOriginalValue( Trackable owner );
                var method = builder.DefineMethod( "GetOriginalValue", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.Standard );

                method.SetReturnType( property.PropertyType );
                method.SetParameters( typeof( Trackable ) );
                method.DefineParameter( 1, ParameterAttributes.None, "owner" );

                Generate( method, x =>
                {
                    x.Emit( OpCodes.Ldarg_1 );
                    x.Emit( OpCodes.Castclass, owner );
                    x.Emit( OpCodes.Ldfld, property.OriginalField );
                    x.Emit( OpCodes.Ret );
                } );

                builder.DefineMethodOverride( method, builder.BaseType.GetMethod( method.Name )! );
            }
            {   // void SetOriginalValue( Trackable owner, T value );
                var method = builder.DefineMethod( "SetOriginalValue", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.Standard );

                method.SetParameters( typeof( Trackable ), property.PropertyType );
                method.DefineParameter( 1, ParameterAttributes.None, "owner" );
                method.DefineParameter( 2, ParameterAttributes.None, "value" );

                Generate( method, x =>
                {
                    x.Emit( OpCodes.Ldarg_1 );
                    x.Emit( OpCodes.Castclass, owner );
                    x.Emit( OpCodes.Ldarg_2 );
                    x.Emit( OpCodes.Stfld, property.OriginalField );
                    x.Emit( OpCodes.Ret );
                } );

                builder.DefineMethodOverride( method, builder.BaseType.GetMethod( method.Name )! );
            }
            {   // T GetCurrentValue( Trackable owner );
                var method = builder.DefineMethod( "GetCurrentValue", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.Standard );

                method.SetReturnType( property.PropertyType );
                method.SetParameters( typeof( Trackable ) );
                method.DefineParameter( 1, ParameterAttributes.None, "owner" );

                Generate( method, x =>
                {
                    x.Emit( OpCodes.Ldarg_1 );
                    x.Emit( OpCodes.Castclass, owner );
                    x.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                    x.Emit( OpCodes.Ret );
                } );

                builder.DefineMethodOverride( method, builder.BaseType.GetMethod( method.Name )! );
            }
            if ( property.BaseSetMethod is not null )
            {
                var method = builder.DefineMethod( "SetCurrentValue", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final, CallingConventions.Standard );

                method.SetParameters( typeof( Trackable ), property.PropertyType );
                method.DefineParameter( 1, ParameterAttributes.None, "owner" );
                method.DefineParameter( 2, ParameterAttributes.None, "value" );

                Generate( method, x =>
                {
                    x.Emit( OpCodes.Ldarg_1 );
                    x.Emit( OpCodes.Castclass, owner );
                    x.Emit( OpCodes.Ldarg_2 );
                    x.Emit( OpCodes.Call, property.BaseSetMethod );
                    x.Emit( OpCodes.Ret );
                } );

                builder.DefineMethodOverride( method, builder.BaseType.GetMethod( method.Name )! );
            }
        }


        private static void OverrideConstructors( TypeBuilder type, List<TrackableProperty> properties )
        {
            FieldBuilder propertiesField;
            var flags = MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig;
            var ctor = type.DefineConstructor( flags, CallingConventions.Standard, null )
                           .GetILGenerator();

            propertiesField = type.DefineField( "TrackableProperties", typeof( TrackablePropertyDictionary ), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly );
            ctor.Emit( OpCodes.Newobj, propertiesField.FieldType.GetConstructor( Type.EmptyTypes )! );

            if ( properties.Count > 0 )
            {
                ctor.Emit( OpCodes.Dup );
            }
            ctor.Emit( OpCodes.Stsfld, propertiesField );

            var addPropertyMethod = propertiesField.FieldType.GetMethod( nameof( TrackablePropertyDictionary.Add ) )!;

            for ( var i = 0; i < properties.Count; ++i )
            {
                var property = properties[ i ];
                DefineTrackablePropertyType( type, property );

                if ( i + 1 < properties.Count )
                {
                    ctor.Emit( OpCodes.Dup );
                }

                ctor.Emit( OpCodes.Ldsfld, property.TrackablePropertyField );
                ctor.Emit( OpCodes.Call, addPropertyMethod );
            }

            ctor.Emit( OpCodes.Ret );

            var constructors = typeof( T ).GetConstructors( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            var propertiesInstanceField = typeof( Trackable ).GetField( "m_trackedProperties", BindingFlags.NonPublic | BindingFlags.Instance )!;

            if ( constructors.Length == 0 )
            {
                ctor = type.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes )
                           .GetILGenerator();

                ctor.Emit( OpCodes.Ldarg_0 );
                ctor.Emit( OpCodes.Ldsfld, propertiesField );
                ctor.Emit( OpCodes.Stfld, propertiesInstanceField );
                ctor.Emit( OpCodes.Ret );
            }
            else
            {
                foreach ( var baseCtor in constructors )
                {
                    var parameters = baseCtor.GetParameters();
                    ctor = type.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, parameters.Select( x => x.ParameterType ).ToArray() )
                               .GetILGenerator();

                    ctor.Emit( OpCodes.Ldarg_0 );
                    ctor.Emit( OpCodes.Call, Trackable.BeginInitMethod );
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
                    ctor.Emit( OpCodes.Ldarg_0 );
                    ctor.Emit( OpCodes.Ldsfld, propertiesField );
                    ctor.Emit( OpCodes.Stfld, propertiesInstanceField );

                    // Call SetValue or SetTrackableValue for each readonly property
                    foreach ( var property in properties.Where( x => x.IsReadOnly ) )
                    {
                        if ( !property.IsTrackable )
                        {
                            // Set the original value to the current value
                            ctor.Emit( OpCodes.Ldarg_0 );
                            ctor.Emit( OpCodes.Ldarg_0 );
                            ctor.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                            ctor.Emit( OpCodes.Stfld, property.OriginalField );
                        }
                        else
                        {
                            // Trackable{Property}.SetTrackableValue( this, value: base.{Property} );
                            ctor.Emit( OpCodes.Ldsfld, property.TrackablePropertyField );
                            ctor.Emit( OpCodes.Ldarg_0 );
                            ctor.Emit( OpCodes.Ldarg_0 );
                            ctor.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                            ctor.Emit( OpCodes.Call, property.TrackablePropertyFieldType.GetMethod( "SetTrackableValue", BindingFlags.NonPublic | BindingFlags.Instance )! );
                            ctor.Emit( OpCodes.Pop ); // The method above returns boolean we don't use
                        }
                    }

                    ctor.Emit( OpCodes.Ldarg_0 );
                    ctor.Emit( OpCodes.Call, Trackable.EndInitMethod );
                    ctor.Emit( OpCodes.Ret );
                }
            }
        }


        private static void OverrideTrackableProperty( TypeBuilder type, TrackableProperty property )
        {
            Debug.Assert( !property.IsReadOnly );
            Debug.Assert( property.TrackablePropertyField is not null );
            Debug.Assert( property.OriginalField is not null );
            Debug.Assert( property.BaseSetMethod is not null );

            property.SetMethod = type.DefineMethod( name: $"Set{property.Name}",
                                                    attributes: property.BaseSetMethod.Attributes | MethodAttributes.Final,
                                                    returnType: null,
                                                    parameterTypes: [ property.PropertyType ] );

            property.SetMethod.DefineParameter( 1, ParameterAttributes.None, "value" );

            Generate( property.SetMethod, x =>
            {
                // PropertyTracking.SetValue( this, value );
                x.Emit( OpCodes.Ldsfld, property.TrackablePropertyField );
                x.Emit( OpCodes.Ldarg_0 );
                x.Emit( OpCodes.Ldarg_1 );
                x.Emit( OpCodes.Call, property.TrackablePropertyFieldType.GetMethod( "SetValue" )! );
                x.Emit( OpCodes.Ret );
            } );

            type.DefineMethodOverride( property.SetMethod, property.BaseSetMethod );
        }


        private static void OverrideObservableProperty( TypeBuilder type, Property property )
        {
            if ( property.BaseSetMethod is null )
                throw new InvalidProgramException( $"The property {property.Info} in {property.Info.DeclaringType} is readonly and cannot be observable." );

            var propertyChangedEventArgs = type.DefineField( property.Name + "PropertyChangedEventArgs", typeof( PropertyChangedEventArgs ), FieldAttributes.Static | FieldAttributes.Private );
            var method = type.DefineMethod( name: $"Set{property.Name}",
                                                    attributes: property.BaseSetMethod.Attributes | MethodAttributes.Final,
                                                    returnType: null,
                                                    parameterTypes: [ property.PropertyType ] );

            method.DefineParameter( 1, ParameterAttributes.None, "value" );

            // if ( Equality.IsDifferent( base.Property, value ) )
            // {
            //      base.Property = value;
            //      RaisePropertyChanged( "Property", ref _propertyChangedEventArgs );
            // }
            Generate( method, x =>
            {
                var IL_RETURN = x.DefineLabel();

                // if ( Equality.IsDifferent( base.Property, value ) )
                x.Emit( OpCodes.Ldarg_0 );
                x.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                x.Emit( OpCodes.Ldarg_1 );
                x.Emit( OpCodes.Call, typeof( Equality ).GetMethod( "IsDifferent" )!.MakeGenericMethod( property.PropertyType ) );
                x.Emit( OpCodes.Brfalse_S, IL_RETURN );

                // base.Property = value;
                x.Emit( OpCodes.Ldarg_0 );
                x.Emit( OpCodes.Ldarg_1 );
                x.Emit( OpCodes.Call, property.BaseSetMethod );

                // RaisePropertyChanged( "Property", ref _propertyChangedEventArgs );
                x.Emit( OpCodes.Ldarg_0 );
                x.Emit( OpCodes.Ldstr, property.Name );
                x.Emit( OpCodes.Ldsflda, propertyChangedEventArgs );
                x.Emit( OpCodes.Call, ObservableObject.RaisePropertyChangedMethod );

                x.MarkLabel( IL_RETURN );
                x.Emit( OpCodes.Ret );
            } );

            type.DefineMethodOverride( method, property.BaseSetMethod );
        }


        private static void OverrideEquals( TypeBuilder type, List<TrackableProperty> properties )
        {
            var method = type.DefineMethod( name: "Equals",
                                            attributes: MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                                            returnType: typeof( bool ),
                                            parameterTypes: [ typeof( T ) ] );
            method.DefineParameter( 1, ParameterAttributes.None, "obj" );

            Generate( method, x =>
            {
                var IL_NOT_EQUAL = x.DefineLabel();

                x.Emit( OpCodes.Ldarg_1 );
                x.Emit( OpCodes.Brfalse, IL_NOT_EQUAL );

                foreach ( var property in properties )
                {
                    var arg1 = new Action<ILGenerator>( x =>
                    {
                        x.Emit( OpCodes.Ldarg_0 );
                        x.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                    } );
                    var arg2 = new Action<ILGenerator>( x =>
                    {
                        x.Emit( OpCodes.Ldarg_1 );
                        x.Emit( OpCodes.Callvirt, property.BaseGetMethod );
                    } );

                    if ( property.ReferenceOnly )
                    {
                        x.EmitReferenceEquals( property.PropertyType, arg1, arg2 );
                    }
                    else
                    {
                        x.EmitEquals( property.PropertyType, arg1, arg2 );
                    }
                    x.Emit( OpCodes.Brfalse, IL_NOT_EQUAL );
                }

                x.Emit( OpCodes.Ldc_I4_1 );
                x.Emit( OpCodes.Ret );

                x.MarkLabel( IL_NOT_EQUAL );
                x.Emit( OpCodes.Ldc_I4_0 );
                x.Emit( OpCodes.Ret );
            } );
        }


        private static void Generate( MethodBuilder method, Action<ILGenerator> generator ) => generator( method.GetILGenerator() );


        private static void Generate( ConstructorBuilder method, Action<ILGenerator> generator ) => generator( method.GetILGenerator() );
    }
}
