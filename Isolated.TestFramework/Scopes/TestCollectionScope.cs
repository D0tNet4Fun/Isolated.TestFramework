using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Isolated.TestFramework.Scopes
{
    internal class TestCollectionScope : IsolationScope
    {
        private readonly ITestCollection _testCollection;
        private readonly IMessageSinkWithEvents _messageSinkWithEvents;

        public TestCollectionScope(ITestCollection testCollection, IMessageSinkWithEvents messageSinkWithEvents, IsolatedContext isolatedContext, TaskFactory dispositionTaskFactory, IMessageSink diagnosticMessageSink)
            : base(isolatedContext, dispositionTaskFactory, diagnosticMessageSink)
        {
            _testCollection = testCollection;
            _messageSinkWithEvents = messageSinkWithEvents;
            _messageSinkWithEvents.TestCollectionFinished += MessageSinkWithEventsOnTestCollectionFinished;
        }

        private void MessageSinkWithEventsOnTestCollectionFinished(object sender, ITestCollection e)
        {
            if (_testCollection == e) SetFinalEvent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageSinkWithEvents.TestCollectionFinished -= MessageSinkWithEventsOnTestCollectionFinished;
            }
            base.Dispose(disposing);
        }
    }
}