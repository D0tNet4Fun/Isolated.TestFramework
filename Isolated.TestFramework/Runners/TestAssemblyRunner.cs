using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Isolated.TestFramework.Behaviors;
using Isolated.TestFramework.Remoting;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Runners
{
    internal class TestAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly TestCaseDeserializerArgs _testCaseDeserializerArgs;
        private readonly IMessageSinkWithEvents _messageSyncWithEvents;
        private IIsolationBehavior _isolationBehavior;
        private IsolatedDispositionTaskScheduler _taskScheduler;
        private TaskFactory _dispositionTaskFactory;
        private Type[] _appDomainFixtureTypes;

        public TestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, TestCaseDeserializerArgs testCaseDeserializerArgs)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
            _testCaseDeserializerArgs = testCaseDeserializerArgs;
            _messageSyncWithEvents = new MessageSinkWithEvents(executionMessageSink, diagnosticMessageSink);
            ExecutionMessageSink = _messageSyncWithEvents; // the ExecutionMessageSink is used to create the base message bus
        }

        protected override IMessageBus CreateMessageBus()
        {
            // the message bus will have to cross app domains so it has to be a remote object
            return new RemoteMessageBus(base.CreateMessageBus());
        }

        protected override async Task AfterTestAssemblyStartingAsync()
        {
            Aggregator.Run(() =>
            {
                ConfigureIsolationBehavior();
                InitializeAsyncDisposition();
                GetAppDomainFixtureTypes();
            });

            await base.AfterTestAssemblyStartingAsync();
        }

        private void ConfigureIsolationBehavior()
        {
            var attributeInfo = TestAssembly.Assembly.GetCustomAttributes(typeof(IsolationBehaviorAttribute)).SingleOrDefault();
            if (attributeInfo == null) return;

            var args = attributeInfo.GetConstructorArguments().ToArray();
            var isolationLevel = (IsolationLevel)args[0];
            switch (isolationLevel)
            {
                case IsolationLevel.Default:
                    return;
                case IsolationLevel.Custom:
                    var type = (Type)args[1];
                    _isolationBehavior = (IIsolationBehavior)ObjectFactory.CreateInstance(type, null);
                    break;
                case IsolationLevel.Collections:
                    _isolationBehavior = new IsolateTestCollectionBehavior();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown isolation level: {isolationLevel}");
            }
        }

        private void GetAppDomainFixtureTypes()
        {
            var isolatedFixtureAttributes = TestAssembly.Assembly.GetCustomAttributes(typeof(IsolatedFixtureAttribute));
            _appDomainFixtureTypes = isolatedFixtureAttributes
                .SelectMany(a => a.GetNamedArgument<Type[]>(nameof(IsolatedFixtureAttribute.FixtureTypes)))
                .Distinct()
                .ToArray();
            foreach (var fixtureType in _appDomainFixtureTypes)
            {
                Aggregator.Run(() =>
                {
                    var defaultConstructor = fixtureType.GetConstructors().SingleOrDefault(c => !c.GetParameters().Any());
                    if (defaultConstructor == null)
                        throw new InvalidOperationException($"App domain fixture type '{fixtureType}' does not have a public default constructor.");
                });
            }
        }

        private void InitializeAsyncDisposition()
        {
            if (_isolationBehavior != null)
            {
                _taskScheduler = new IsolatedDispositionTaskScheduler();
                _dispositionTaskFactory = new TaskFactory(_taskScheduler);
            }
        }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            return new TestCollectionRunner(testCollection, testCases.ToList(), DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource, _messageSyncWithEvents, _testCaseDeserializerArgs, _isolationBehavior, _appDomainFixtureTypes, _dispositionTaskFactory).RunAsync();
        }

        protected override async Task BeforeTestAssemblyFinishedAsync()
        {
            await base.BeforeTestAssemblyFinishedAsync();
            DisposeTaskScheduler();
        }

        private void DisposeTaskScheduler()
        {
            if (_taskScheduler != null)
            {
                Aggregator.Run(() => _taskScheduler.Dispose());
            }
        }
    }
}