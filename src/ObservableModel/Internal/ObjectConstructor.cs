using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ObservableModel
{
    internal static class ObjectConstructor
    {
        private delegate object Factory();
        private delegate object Factory<T>( T parameter );


        public static object Create( Type type ) => Bind<Factory>( type )();


        public static object Create<T>( Type type, T arg ) => Bind<Factory<T>>( type )( arg );


        private static T Bind<T>( Type type ) where T : Delegate => (T)s_binders.GetOrAdd( (type, typeof( T )), s_binderFactory ).Constructor;


        private static readonly ConcurrentDictionary<(Type, Type), Binder> s_binders = new();
        private static readonly Func<(Type Type, Type Factory), Binder> s_binderFactory = new( x =>
        {
            Debug.Assert( typeof( Delegate ).IsAssignableFrom( x.Factory ) );

            if ( x.Factory.IsGenericType )
            {
                var argument = x.Factory.GetGenericArguments()[ 0 ];

                if ( argument.IsValueType && argument.FullName?.StartsWith( "System.ValueTuple`", StringComparison.Ordinal ) == true )
                    return (Binder)Activator.CreateInstance( typeof( BinderVarArg<> ).MakeGenericType( argument ), x.Type )!;

                return (Binder)Activator.CreateInstance( typeof( Binder<> ).MakeGenericType( argument ), x.Type )!;
            }

            return new Binder( x.Type );
        } );


        class Binder
        {
            public Binder( Type type )
            {
                Type = type;
            }


            public Type Type { get; }


            public Delegate Constructor
            {
                get
                {
                    if ( m_constructor is null )
                    {
                        Debugger.NotifyOfCrossThreadDependency();

                        lock ( m_lock )
                        {
                            m_constructor ??= Bind();
                        }
                    }

                    return m_constructor;
                }
            }


            protected virtual Delegate Bind()
            {
                var constructor = Type.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, Type.EmptyTypes, modifiers: null ) ?? throw new InvalidProgramException( $"No default constructor found in {Type}." );
                var method = new DynamicMethod( Guid.NewGuid().ToString( "N" ), typeof( object ), Type.EmptyTypes, constructor.Module, skipVisibility: true );
                var il = method.GetILGenerator();

                il.Emit( OpCodes.Newobj, constructor );
                il.Emit( OpCodes.Ret );

                return method.CreateDelegate( typeof( Factory ) );
            }


            private readonly Lock m_lock = new();
            private Delegate? m_constructor;
        }


        class Binder<T> : Binder
        {
            public Binder( Type type ) : base( type )
            {
            }


            protected override Delegate Bind()
            {
                var constructor = Type.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, [ typeof( T ) ], modifiers: null ) ?? throw new InvalidProgramException( $"No constructor found in {Type} matching ({typeof( T )})." );
                var method = new DynamicMethod( Type.Name, typeof( object ), [ typeof( T ) ], constructor.Module, skipVisibility: true );

                var il = method.GetILGenerator();

                il.Emit( OpCodes.Ldarg_0 );
                il.Emit( OpCodes.Newobj, constructor );
                il.Emit( OpCodes.Ret );

                return method.CreateDelegate( typeof( Factory<T> ) );
            }
        }


        class BinderVarArg<T> : Binder where T : struct, ITuple
        {
            public BinderVarArg( Type type ) : base( type )
            {
            }


            protected override Delegate Bind()
            {
                var arguments = new List<Type>( typeof( T ).GetGenericArguments() );
                var tupleTypes = new List<Type>
                {
                    typeof( T )
                };

                if ( arguments.Count == 8 )
                {
                Next:
                    // Deconstruct the Rest field types
                    var nestedTupleType = arguments[ ^1 ];
                    var nestedTupleGenericArgs = nestedTupleType.GetGenericArguments();

                    tupleTypes.Add( nestedTupleType );

                    arguments.RemoveAt( arguments.Count - 1 );
                    arguments.AddRange( nestedTupleGenericArgs );

                    if ( nestedTupleGenericArgs.Length == 8 )
                    {
                        // There are more nested args
                        goto Next;
                    }
                }

                var parameters = arguments.ToArray();
                var constructor = Type.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, parameters, modifiers: null ) ?? throw new InvalidProgramException( $"No constructor found in {Type} matching ({String.Join( ", ", arguments )})." );
                var method = new DynamicMethod( Guid.NewGuid().ToString( "N" ), typeof( object ), [ typeof( T ) ], constructor.Module, skipVisibility: true );
                var il = method.GetILGenerator();

                for ( var i = 0; i < parameters.Length; ++i )
                {
                    il.Emit( OpCodes.Ldarga_S, 0 );

                    var currentTupleType = typeof( T );

                    for ( var j = 0; j < i / 7; ++j )
                    {
                        il.Emit( OpCodes.Ldflda, tupleTypes[ j ].GetField( "Rest" )! );
                        currentTupleType = tupleTypes[ j + 1 ];
                    }

                    il.Emit( OpCodes.Ldfld, currentTupleType.GetField( "Item" + ( ( i % 7 ) + 1 ) )! );
                }

                il.Emit( OpCodes.Newobj, constructor );
                il.Emit( OpCodes.Ret );

                return method.CreateDelegate( typeof( Factory<T> ) );
            }
        }
    }
}
