using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Scopes
{
    internal class IsolationScope : IDisposable
    {
        private readonly IsolatedContext _isolatedContext;
        private readonly TaskFactory _dispositionTaskFactory;
        private readonly IMessageSink _diagnosticMessageSink;
        private readonly ManualResetEventSlim _finalEventRaised = new ManualResetEventSlim(false);

        protected IsolationScope(IsolatedContext isolatedContext, TaskFactory dispositionTaskFactory, IMessageSink diagnosticMessageSink)
        {
            _isolatedContext = isolatedContext;
            _dispositionTaskFactory = dispositionTaskFactory;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected void SetFinalEvent() => _finalEventRaised.Set();

        private void Wait(CancellationToken cancellationToken)
        {
            _finalEventRaised.Wait(cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _finalEventRaised?.Dispose();
                _isolatedContext?.Dispose();
            }
        }

        public async void DisposeOnCompletionAsync(CancellationToken waitCancellationToken)
        {
            try
            {
                await _dispositionTaskFactory.StartNew(args =>
                {
                    var tuple = (Tuple<IsolationScope, CancellationToken>)args;
                    var scope = tuple.Item1;
                    var cancellationToken = tuple.Item2;
                    try
                    {
                        scope.Wait(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    scope.Dispose();
                }, Tuple.Create(this, waitCancellationToken), CancellationToken.None);
            }
            catch (Exception e)
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Error disposing isolated context async: {e}"));
            }
        }
    }
}