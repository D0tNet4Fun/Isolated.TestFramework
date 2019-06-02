using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Isolated.TestFramework.Behaviors;
using Isolated.TestFramework.Events;
using Isolated.TestFramework.Remoting;
using Isolated.TestFramework.Scopes;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Runners
{
    internal class TestCollectionRunner : XunitTestCollectionRunner
    {
        private readonly IMessageSinkWithEvents _meeMessageSinkWithEvents;
        private readonly TestCaseDeserializerArgs _testCaseDeserializerArgs;
        private readonly AppDomainEventListener _appDomainEventListener;

        public TestCollectionRunner(ITestCollection testCollection, IReadOnlyList<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IMessageSinkWithEvents meeMessageSinkWithEvents, TestCaseDeserializerArgs testCaseDeserializerArgs, IIsolationBehavior isolationBehavior, AppDomainEventListener appDomainEventListener)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _meeMessageSinkWithEvents = meeMessageSinkWithEvents;
            _testCaseDeserializerArgs = testCaseDeserializerArgs;
            _appDomainEventListener = appDomainEventListener;

            RunIsolated = isolationBehavior?.IsolateTestCollection(testCollection, testCases) ?? false;
        }

        public bool RunIsolated { get; }

        public new IReadOnlyList<IXunitTestCase> TestCases => (IReadOnlyList<IXunitTestCase>)base.TestCases;

        public new async Task<RunSummary> RunAsync()
        {
            if (!RunIsolated) return await base.RunAsync();

            try
            {
                return await RunIsolatedAsync();
            }
            catch (Exception e)
            {
                return FailAllTestsInCollection(e);
            }
        }

        private async Task<RunSummary> RunIsolatedAsync()
        {
            using (var scope = new TestCollectionScope(TestCollection, _meeMessageSinkWithEvents))
            using (var isolated = new Isolated(_appDomainEventListener))
            {
                var remoteTestCases = isolated.CreateRemoteTestCases(TestCases, _testCaseDeserializerArgs);
                var remoteCancellationTokenSource = isolated.CreateRemoteCancellationTokenSource(CancellationTokenSource);
                var runnerArgs = new object[] {TestCollection, remoteTestCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer.GetType(), remoteCancellationTokenSource};
                var runSummary = await isolated.CreateInstanceAndRunAsync<RemoteTestCollectionRunner>(runnerArgs, x => x.RunAsync());

                // wait for the scope before disposing the isolated instance, but only if we made it this far
                await scope.WaitAsync(CancellationTokenSource.Token);
                return runSummary;
            }
        }

        private RunSummary FailAllTestsInCollection(Exception e)
        {
            MessageBus.QueueMessage(new ErrorMessage(TestCases, e));
            return new RunSummary
            {
                Total = TestCases.Count,
                Failed = TestCases.Count
            };
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class RemoteTestCollectionRunner : XunitTestCollectionRunner
        {
            private readonly RemoteCancellationTokenSource _remoteCancellationTokenSource;

            public RemoteTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, Type testCaseOrdererType, RemoteCancellationTokenSource remoteCancellationTokenSource)
                : base(testCollection, testCases, diagnosticMessageSink, messageBus, null, null, null)
            {
                _remoteCancellationTokenSource = remoteCancellationTokenSource;
                TestCaseOrderer = ObjectFactory.CreateTestCaseOrderer(testCaseOrdererType, diagnosticMessageSink);
                Aggregator = new ExceptionAggregator();
                CancellationTokenSource = remoteCancellationTokenSource.CancellationTokenSource;
            }

            protected override async Task BeforeTestCollectionFinishedAsync()
            {
                await base.BeforeTestCollectionFinishedAsync();
                _remoteCancellationTokenSource.Dispose();
            }
        }
    }
}