using System.Threading;
using System.Threading.Tasks;

namespace Isolated.TestFramework.Scopes
{
    internal class IsolationScope
    {
        private readonly TaskCompletionSource<object> _finalEventRaised = new TaskCompletionSource<object>();

        protected IsolationScope()
        {
        }

        protected void SetFinalEvent() => _finalEventRaised.SetResult(null);

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            var registration = cancellationToken.Register(() => _finalEventRaised.SetCanceled());
            _finalEventRaised.Task.ContinueWith(t => registration.Dispose(), CancellationToken.None);
            return _finalEventRaised.Task;
        }
    }
}