using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AppDomainToolkit;
using Isolated.TestFramework.Remoting;
using Isolated.TestFramework.Scopes;
using Xunit.Sdk;
using IDisposable = System.IDisposable;

namespace Isolated.TestFramework
{
    internal class Isolated : MarshalByRefObject, IDisposable
    {
        private readonly IsolationScope _scope;
        private readonly AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> _appDomainContext;

        public Isolated(IsolationScope scope)
        {
            _scope = scope;
            _appDomainContext = AppDomainContext.Create(AppDomain.CurrentDomain.SetupInformation);
            CallerAppDomainId = AppDomain.CurrentDomain.Id;
        }

        public int CallerAppDomainId { get; }

        public IEnumerable<IXunitTestCase> CreateRemoteTestCases(IEnumerable<IXunitTestCase> testCases, TestCaseDeserializerArgs testCaseDeserializerArgs)
        {
            var remoteObjectFactory = new RemoteObjectFactory(_appDomainContext.Domain, testCaseDeserializerArgs);
            var remoteTestCases = testCases.Select(testCase => (IXunitTestCase)remoteObjectFactory.CreateTestCaseFrom(testCase)).ToArray();
            return remoteTestCases;
        }

        public async Task<RunSummary> CreateInstanceAndRunAsync<TRunner>(object[] runnerArgs, Expression<Func<TRunner, Task<RunSummary>>> runAsyncExpression)
        {
            var methodInfo = ((MethodCallExpression)runAsyncExpression.Body).Method;

            var remoteTaskCompletionSource = new RemoteTaskCompletionSource<SerializableRunSummary>();
            RemoteAction.Invoke(
                _appDomainContext.Domain,
                CallerAppDomainId,
                runnerArgs,
                remoteTaskCompletionSource,
                methodInfo,
                async (callerAppDomainId, args, taskCompletionSource, runAsyncMethod) =>
                {
                    try
                    {
                        if (callerAppDomainId == AppDomain.CurrentDomain.Id)
                            throw new InvalidOperationException("The action is running in the default app domain instead of being run in the foreign app domain.");

                        var runner = (TRunner)Activator.CreateInstance(typeof(TRunner), args);
                        var runSummary = await (Task<RunSummary>)runAsyncMethod.Invoke(runner, null);
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
            return serializableRunSummary.AsRunSummary();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _appDomainContext.Dispose();
        }
    }
}