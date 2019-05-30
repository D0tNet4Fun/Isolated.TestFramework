using System;
using System.Threading;

namespace Isolated.TestFramework.Remoting
{
    internal class RemoteCancellationTokenSource : MarshalByRefObject, IDisposable
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public void Cancel() => CancellationTokenSource.Cancel();

        public void Dispose()
        {
            CancellationTokenSource?.Dispose();
        }
    }
}