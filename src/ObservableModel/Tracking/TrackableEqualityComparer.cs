using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableModel
{
    internal interface ITrackableEquatable<T> where T : Trackable
    {
        bool Equals( T other );
    }

    public sealed class TrackableEqualityComparer<T> : EqualityComparer<T> where T : Trackable
    {
        public static readonly new TrackableEqualityComparer<T> Default = new();

        private TrackableEqualityComparer()
        {
        }

        public override bool Equals( T? x, T? y )
        {
            if ( x is not null && y is not null )
                return ( (ITrackableEquatable<T>)x ).Equals( y );

            return false;
        }

        public override int GetHashCode( [DisallowNull] T obj ) => throw new NotSupportedException();
    }
}
