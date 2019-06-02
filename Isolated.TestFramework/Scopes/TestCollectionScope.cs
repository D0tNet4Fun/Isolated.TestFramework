using System;
using Xunit.Abstractions;

namespace Isolated.TestFramework.Scopes
{
    internal class TestCollectionScope : IsolationScope, IDisposable
    {
        private readonly ITestCollection _testCollection;
        private readonly IMessageSinkWithEvents _messageSinkWithEvents;

        public TestCollectionScope(ITestCollection testCollection, IMessageSinkWithEvents messageSinkWithEvents)
        {
            _testCollection = testCollection;
            _messageSinkWithEvents = messageSinkWithEvents;
            _messageSinkWithEvents.TestCollectionFinished += MessageSinkWithEventsOnTestCollectionFinished;
        }

        private void MessageSinkWithEventsOnTestCollectionFinished(object sender, ITestCollection e)
        {
            if (_testCollection == e) SetFinalEvent();
        }

        public void Dispose()
        {
            _messageSinkWithEvents.TestCollectionFinished -= MessageSinkWithEventsOnTestCollectionFinished;
        }
    }
}