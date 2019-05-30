using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Remoting
{
    internal class RemoteMessageBus : LongLivedMarshalByRefObject, IMessageBus
    {
        private readonly IMessageBus _messageBus;

        public RemoteMessageBus(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        public void Dispose() => _messageBus.Dispose();

        public bool QueueMessage(IMessageSinkMessage message) => _messageBus.QueueMessage(message);
    }
}