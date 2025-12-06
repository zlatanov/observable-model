using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ObservableModel
{
    internal static class MethodBuilderExtensions
    {
        public static void EmitReferenceEquals( this ILGenerator generator, Type type, Action<ILGenerator> arg1, Action<ILGenerator> arg2 )
        {
            if ( type.IsValueType )
            {
                // The values can never have reference equality
                generator.Emit( OpCodes.Ldc_I4_0 );
            }
            else
            {
                arg1( generator );
                arg2( generator );
                generator.Emit( OpCodes.Ceq );
            }
        }


        public static void EmitEquals( this ILGenerator generator, Type type, Action<ILGenerator> arg1, Action<ILGenerator> arg2 )
        {
            if ( Nullable.GetUnderlyingType( type ) is null )
            {
                switch ( Type.GetTypeCode( type ) )
                {
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        arg1( generator );
                        arg2( generator );

                        generator.Emit( OpCodes.Ceq );
                        return;
                }
            }

            var comparer = typeof( EqualityComparer<> ).MakeGenericType( type );

            generator.Emit( OpCodes.Call, comparer.GetProperty( "Default" )!.GetGetMethod()! );
            arg1( generator );
            arg2( generator );
            generator.Emit( OpCodes.Callvirt, comparer.GetMethod( "Equals", [ type, type ] )! );
        }
    }
}
