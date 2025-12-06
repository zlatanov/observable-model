using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ObservableModel
{
    internal static class Equality
    {
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool IsDifferent<T>( T x, T y ) => DifferentComparer<T>.IsDifferent( x, y );

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static EqualityComparer<T> GetComparer<T>() => DifferentComparer<T>.Current;

        private sealed class DifferentComparer<T> : EqualityComparer<T>
        {
            private static readonly bool IsValueType = typeof( T ).IsValueType;
            public static readonly DifferentComparer<T> Current = new();

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public static bool IsDifferent( [AllowNull] T x, [AllowNull] T y )
            {
                if ( IsValueType )
                    return !Default.Equals( x, y );

                return IsClassDifferent( x, y );
            }

            [MethodImpl( MethodImplOptions.NoInlining )]
            private static bool IsClassDifferent( [AllowNull] T x, [AllowNull] T y ) => !ReferenceEquals( x, y ) || !Default.Equals( x, y );

            public override bool Equals( [AllowNull] T x, [AllowNull] T y ) => !IsDifferent( x, y );

            public override int GetHashCode( [DisallowNull] T obj ) => Default.GetHashCode( obj );
        }
    }
}
