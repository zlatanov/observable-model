using System;

namespace ObservableModel
{
    internal static class Identity<T>
    {
        public static readonly Func<T, T> Function = x => x;
    }
}
