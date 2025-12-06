using System;
using System.Threading;

namespace ObservableModel
{
    internal sealed class Disposable : IDisposable
    {
        public static readonly Disposable Empty = new();


        public Disposable( Action action )
        {
            m_action = action;
        }


        private Disposable()
        {
        }


        public void Dispose() => Interlocked.Exchange( ref m_action, null )?.Invoke();


        private Action? m_action;
    }
}
