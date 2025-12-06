using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace ObservableModel
{
    internal static class Dynamic
    {
        private static readonly ModuleBuilder Module;
        private static readonly HashSet<string> TypeNames = new( StringComparer.OrdinalIgnoreCase );

        public static readonly object Lock = new();


        static Dynamic()
        {
            var assemblyName = new AssemblyName( "ObservableModel.Dynamic" );
            var assembly = AssemblyBuilder.DefineDynamicAssembly( assemblyName, AssemblyBuilderAccess.Run );

            Module = assembly.DefineDynamicModule( assemblyName.Name + ".dll" );
        }


        public static TypeBuilder DefineType( string name, TypeAttributes attributes, Type parent )
        {
            Debug.Assert( Monitor.IsEntered( Lock ) );

            var actualNameIndex = 0;
            var actualName = name;

            while ( !TypeNames.Add( actualName ) )
            {
                actualNameIndex += 1;
                actualName = name + "`" + actualNameIndex;
            }

            return Module.DefineType( actualName, attributes, parent: parent );
        }
    }
}
