using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Isolated.TestFramework.Behaviors;
using Isolated.TestFramework.Events;
using Isolated.TestFramework.Remoting;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Runners
{
    internal class TestAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly TestCaseDeserializerArgs _testCaseDeserializerArgs;
        private readonly IMessageSinkWithEvents _messageSyncWithEvents;
        private Exception _configurationException;
        private IIsolationBehavior _isolationBehavior;
        private AppDomainEventListener _appDomainEventListener;

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
            try
            {
                ConfigureIsolationBehavior();
                ConfigureAppDomainEventListener();
            }
            catch (TargetInvocationException e)
            {
                _configurationException = e.InnerException;
            }
            catch (Exception e)
            {
                _configurationException = e;
            }

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
                case IsolationLevel.Default: return;
                case IsolationLevel.Custom:
                    var type = (Type)args[1];
                    _isolationBehavior = (IIsolationBehavior)ObjectFactory.CreateInstance(type, null);
                    break;
                case IsolationLevel.Collections:
                    _isolationBehavior = new IsolateTestCollectionBehavior();
                    break;
                default:
                    DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"Unknown isolation level: {isolationLevel}"));
                    break;
            }
        }

        private void ConfigureAppDomainEventListener()
        {
            var attributeInfo = TestAssembly.Assembly.GetCustomAttributes(typeof(AppDomainEventListenerAttribute)).SingleOrDefault();
            if (attributeInfo == null) return;

            var type = (Type)attributeInfo.GetConstructorArguments().Single();
            _appDomainEventListener = (AppDomainEventListener)ObjectFactory.CreateInstance(type, null);
        }

        protected override async Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            if (_configurationException != null)
            {
                DiagnosticMessageSink.OnMessage(new DiagnosticMessage($"An error occurred while configuring the isolation behavior: {_configurationException}"));
                messageBus.QueueMessage(new ErrorMessage(new IXunitTestCase[0], _configurationException));
                return new RunSummary();
            }

            using (var taskScheduler = new IsolatedDispositionTaskScheduler())
            {
                var dispositionTaskFactory = new TaskFactory(taskScheduler);
                return await new TestCollectionRunner(testCollection, testCases.ToList(), DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource, _messageSyncWithEvents, _testCaseDeserializerArgs, _isolationBehavior, _appDomainEventListener, dispositionTaskFactory).RunAsync();
            }
        }
    }
}