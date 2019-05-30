using System;
using System.Threading;

namespace Isolated.TestFramework.Scopes
{
    internal class IsolationScope : IDisposable
    {
        private readonly ManualResetEventSlim _finalEventRaised = new ManualResetEventSlim(false);

        protected IsolationScope()
        {

        }

        protected void SetFinalEvent() => _finalEventRaised.Set();

        public void Abort()
        {
            _finalEventRaised.Set();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) _finalEventRaised.Dispose();
        }

        public void Dispose()
        {
            // wait until the final event is received before disposing the scope
            _finalEventRaised.Wait();

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}