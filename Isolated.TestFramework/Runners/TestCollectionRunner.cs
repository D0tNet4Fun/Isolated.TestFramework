using System;
using System.Collections.Generic;
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

        public new async Task<RunSummary> RunAsync()
        {
            if (!RunIsolated) return await base.RunAsync();

            try
            {
                using (var isolated = new Isolated(new TestCollectionScope(TestCollection, _meeMessageSinkWithEvents), _appDomainEventListener))
                {
                    var remoteTestCases = isolated.CreateRemoteTestCases(TestCases,_testCaseDeserializerArgs);
                    var runnerArgs = new object[] { TestCollection, remoteTestCases, DiagnosticMessageSink, MessageBus };
                    return await isolated.CreateInstanceAndRunAsync<RemoteTestCollectionRunner>(runnerArgs, x => x.RunAsync());
                }
            }
            catch (Exception e)
            {
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Error running test collection in isolation: {e}"));
                throw;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class RemoteTestCollectionRunner : XunitTestCollectionRunner
        {
            public RemoteTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus)
                : base(testCollection, testCases, diagnosticMessageSink, messageBus, null, null, null)
            {
                TestCaseOrderer = new DefaultTestCaseOrderer(diagnosticMessageSink);
                Aggregator = new ExceptionAggregator();
                CancellationTokenSource = new CancellationTokenSource();
            }
        }
    }
}