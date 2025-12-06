using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
    {
        public static readonly ReferenceEqualityComparer<T> Default = new();


        public bool Equals( [AllowNull] T o1, [AllowNull] T o2 ) => ReferenceEquals( o1, o2 );


        public int GetHashCode( T o ) => o is null ? 0 : o.GetHashCode();
    }
}
