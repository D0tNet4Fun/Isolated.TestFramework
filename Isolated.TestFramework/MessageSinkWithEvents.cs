using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework
{
    public class MessageSinkWithEvents : IMessageSinkWithEvents
    {
        private readonly IMessageSink _innerMessageSink;
        private readonly IMessageSink _diagnosticMessageSink;

        public MessageSinkWithEvents(IMessageSink innerMessageSink, IMessageSink diagnosticMessageSink)
        {
            _innerMessageSink = innerMessageSink;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public bool OnMessage(IMessageSinkMessage message)
        {
            var result = _innerMessageSink.OnMessage(message);
            try
            {
                OnMessageHandled((dynamic) message);
            }
            catch (Exception e)
            {
                _diagnosticMessageSink?.OnMessage(new DiagnosticMessage($"Failed to handle message {message.GetType().Name}: {e}"));
            }

            return result;
        }

        private void OnMessageHandled(ITestCollectionFinished message) => TestCollectionFinished?.Invoke(this, message.TestCollection);

        // ReSharper disable once UnusedParameter.Local - used by DLR
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void OnMessageHandled(IMessageSinkMessage message)
        {
        }

        public event EventHandler<ITestCollection> TestCollectionFinished;
    }
}