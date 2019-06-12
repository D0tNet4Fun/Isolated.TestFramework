using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly TaskFactory _dispositionTaskFactory;

        public TestCollectionRunner(ITestCollection testCollection, IReadOnlyList<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IMessageSinkWithEvents meeMessageSinkWithEvents, TestCaseDeserializerArgs testCaseDeserializerArgs, IIsolationBehavior isolationBehavior, AppDomainEventListener appDomainEventListener, TaskFactory dispositionTaskFactory)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _meeMessageSinkWithEvents = meeMessageSinkWithEvents;
            _testCaseDeserializerArgs = testCaseDeserializerArgs;
            _appDomainEventListener = appDomainEventListener;
            _dispositionTaskFactory = dispositionTaskFactory;

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
            Isolated isolated = null;
            TestCollectionScope scope;
            try
            {
                isolated = new Isolated(_appDomainEventListener);
                scope = new TestCollectionScope(TestCollection, _meeMessageSinkWithEvents, isolated, _dispositionTaskFactory, DiagnosticMessageSink); // owns the isolated instance
            }
            catch(Exception)
            {
                isolated?.Dispose();
                throw;
            }
            
            try
            {
                var remoteTestCases = isolated.CreateRemoteTestCases(TestCases, _testCaseDeserializerArgs);
                var remoteCancellationTokenSource = isolated.CreateRemoteCancellationTokenSource(CancellationTokenSource);
                var runnerArgs = new object[] {TestCollection, remoteTestCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer.GetType(), remoteCancellationTokenSource, Aggregator.ToException()};
                var runSummary = await isolated.CreateInstanceAndRunAsync<RemoteTestCollectionRunner>(runnerArgs, RemoteTestCollectionRunner.MethodRunAsync);

                // now that the run summary is available schedule the disposition of the scope as soon as it is completed
                scope.DisposeOnCompletionAsync(CancellationTokenSource.Token);
                return runSummary;
            }
            catch (Exception)
            {
                scope.Dispose();
                throw;
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
            public static readonly MethodInfo MethodRunAsync = typeof(RemoteTestCollectionRunner).GetMethod(nameof(RunAsync));

            private readonly RemoteCancellationTokenSource _remoteCancellationTokenSource;

            public RemoteTestCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, Type testCaseOrdererType, RemoteCancellationTokenSource remoteCancellationTokenSource, Exception aggregatorException)
                : base(testCollection, testCases, diagnosticMessageSink, messageBus, null, null, null)
            {
                _remoteCancellationTokenSource = remoteCancellationTokenSource;
                TestCaseOrderer = ObjectFactory.CreateTestCaseOrderer(testCaseOrdererType, diagnosticMessageSink);
                Aggregator = ObjectFactory.CreateExceptionAggregator(aggregatorException);
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