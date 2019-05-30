using System;
using Xunit.Abstractions;

namespace Isolated.TestFramework
{
    public class MessageSinkWithEvents : IMessageSinkWithEvents
    {
        private readonly IMessageSink _innerMessageSink;

        public MessageSinkWithEvents(IMessageSink innerMessageSink)
        {
            _innerMessageSink = innerMessageSink;
        }

        public bool OnMessage(IMessageSinkMessage message)
        {
            var result = _innerMessageSink.OnMessage(message);
            OnMessageHandled((dynamic)message);
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