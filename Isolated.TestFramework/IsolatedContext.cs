using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppDomainToolkit;
using Isolated.TestFramework.Fixtures;
using Isolated.TestFramework.Remoting;
using Xunit;
using Xunit.Sdk;
using IDisposable = System.IDisposable;

namespace Isolated.TestFramework
{
    internal class IsolatedContext : LongLivedMarshalByRefObject, IDisposable
    {
        private readonly Type[] _appDomainFixtureTypes;
        private readonly AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> _appDomainContext;
        private CancellationTokenRegistration _cancellationRegistration;
        private readonly AppDomainFixtureContainer _fixtureContainer;

        public int CallerAppDomainId { get; }

        public IsolatedContext(Type[] appDomainFixtureTypes, ExceptionAggregator aggregator)
        {
            _appDomainFixtureTypes = appDomainFixtureTypes;

            _appDomainContext = CreateAppDomainContext();
            CallerAppDomainId = AppDomain.CurrentDomain.Id;

            if (appDomainFixtureTypes?.Length > 0)
                _fixtureContainer = CreateRemoteFixtureContainer(aggregator);
        }

        private static AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> CreateAppDomainContext()
        {
            return AppDomainContext.Create(AppDomain.CurrentDomain.SetupInformation);
        }

        private AppDomainFixtureContainer CreateRemoteFixtureContainer(ExceptionAggregator aggregator)
        {
            var remoteContainer = RemoteFunc.Invoke(_appDomainContext.Domain,
                _appDomainFixtureTypes,
                aggregator.ToException(),
                (fixtureTypes, exception) =>
                {
                    var exceptionAggregator = ObjectFactory.CreateExceptionAggregator(exception);
                    return new AppDomainFixtureContainer(fixtureTypes, exceptionAggregator);
                });
            remoteContainer.CreateFixtures();
            return remoteContainer;
        }

        public IEnumerable<IXunitTestCase> CreateRemoteTestCases(IEnumerable<IXunitTestCase> testCases, TestCaseDeserializerArgs testCaseDeserializerArgs)
        {
            var remoteObjectFactory = new RemoteObjectFactory(_appDomainContext.Domain, testCaseDeserializerArgs);
            var remoteTestCases = testCases.Select(testCase => (IXunitTestCase)remoteObjectFactory.CreateTestCaseFrom(testCase)).ToArray();
            return remoteTestCases;
        }

        public RemoteCancellationTokenSource CreateRemoteCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            var remoteObjectFactory = new RemoteObjectFactory(_appDomainContext.Domain, null);
            var remoteCancellationTokenSource = remoteObjectFactory.CreateRemoteCancellationTokenSource();
            _cancellationRegistration = cancellationTokenSource.Token.Register(remoteCancellationTokenSource.Cancel);
            return remoteCancellationTokenSource;
        }

        public async Task<RunSummary> CreateRemoteInstanceAndRunAsync<TRunner>(object[] runnerRemoteArgs, MethodInfo runAsyncMethod)
        {
            var remoteTaskCompletionSource = new RemoteTaskCompletionSource<SerializableRunSummary>();
            RemoteAction.Invoke(
                _appDomainContext.Domain,
                CallerAppDomainId,
                runnerRemoteArgs,
                remoteTaskCompletionSource,
                runAsyncMethod,
                async (callerAppDomainId, args, taskCompletionSource, methodInfo) =>
                {
                    try
                    {
                        if (callerAppDomainId == AppDomain.CurrentDomain.Id)
                            throw new InvalidOperationException("The action is running in the initial app domain instead of being run in the new app domain.");

                        var runner = ObjectFactory.CreateInstance<TRunner>(args);
                        var runSummary = await (Task<RunSummary>)methodInfo.Invoke(runner, null);
                        taskCompletionSource.SetResult(new SerializableRunSummary(runSummary));
                    }
                    catch (OperationCanceledException)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                });
            var serializableRunSummary = await remoteTaskCompletionSource.Task;
            return serializableRunSummary.Deserialize();
        }

        public void Dispose()
        {
            _fixtureContainer?.Dispose();
            _cancellationRegistration.Dispose();
            _appDomainContext.Dispose();
        }
    }
}