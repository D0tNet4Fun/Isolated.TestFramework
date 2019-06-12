using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Isolated.TestFramework.Behaviors;
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
        private readonly Type[] _appDomainFixtureTypes;
        private readonly TaskFactory _dispositionTaskFactory;

        public TestCollectionRunner(ITestCollection testCollection, IReadOnlyList<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IMessageSinkWithEvents meeMessageSinkWithEvents, TestCaseDeserializerArgs testCaseDeserializerArgs, IIsolationBehavior isolationBehavior, Type[] appDomainFixtureTypes, TaskFactory dispositionTaskFactory)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _meeMessageSinkWithEvents = meeMessageSinkWithEvents;
            _testCaseDeserializerArgs = testCaseDeserializerArgs;
            _appDomainFixtureTypes = appDomainFixtureTypes;
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
            IsolatedContext isolatedContext = null;
            TestCollectionScope scope;
            try
            {
                isolatedContext = new IsolatedContext(_appDomainFixtureTypes, Aggregator);
                scope = new TestCollectionScope(TestCollection, _meeMessageSinkWithEvents, isolatedContext, _dispositionTaskFactory, DiagnosticMessageSink); // owns the isolated instance
            }
            catch(Exception)
            {
                isolatedContext?.Dispose();
                throw;
            }
            
            try
            {
                var remoteTestCases = isolatedContext.CreateRemoteTestCases(TestCases, _testCaseDeserializerArgs);
                var remoteCancellationTokenSource = isolatedContext.CreateRemoteCancellationTokenSource(CancellationTokenSource);
                var runnerArgs = new object[] {TestCollection, remoteTestCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer.GetType(), remoteCancellationTokenSource, Aggregator.ToException()};
                var runSummary = await isolatedContext.CreateRemoteInstanceAndRunAsync<RemoteTestCollectionRunner>(runnerArgs, RemoteTestCollectionRunner.MethodRunAsync);

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