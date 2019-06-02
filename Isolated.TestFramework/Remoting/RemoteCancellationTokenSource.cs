using System;
using System.Threading;
using Xunit;

namespace Isolated.TestFramework.Remoting
{
    internal class RemoteCancellationTokenSource : LongLivedMarshalByRefObject, IDisposable
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public void Cancel() => CancellationTokenSource.Cancel();

        public void Dispose()
        {
            CancellationTokenSource?.Dispose();
        }
    }
}