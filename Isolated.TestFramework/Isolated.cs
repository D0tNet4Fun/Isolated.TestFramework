using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppDomainToolkit;
using Isolated.TestFramework.Events;
using Isolated.TestFramework.Remoting;
using Xunit;
using Xunit.Sdk;
using IDisposable = System.IDisposable;

namespace Isolated.TestFramework
{
    internal class Isolated : LongLivedMarshalByRefObject, IDisposable
    {
        private readonly AppDomainEventListener _appDomainEventListener;
        private readonly AppDomainContext<AssemblyTargetLoader, PathBasedAssemblyResolver> _appDomainContext;
        private CancellationTokenRegistration _cancellationRegistration;

        public Isolated(AppDomainEventListener appDomainEventListener)
        {
            _appDomainEventListener = appDomainEventListener;

            OnAppDomainLoading();
            _appDomainContext = AppDomainContext.Create(AppDomain.CurrentDomain.SetupInformation);
            OnAppDomainLoaded();
            CallerAppDomainId = AppDomain.CurrentDomain.Id;
        }

        public int CallerAppDomainId { get; }

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

        public async Task<RunSummary> CreateInstanceAndRunAsync<TRunner>(object[] runnerArgs, MethodInfo runAsyncMethod)
        {
            var remoteTaskCompletionSource = new RemoteTaskCompletionSource<SerializableRunSummary>();
            RemoteAction.Invoke(
                _appDomainContext.Domain,
                CallerAppDomainId,
                runnerArgs,
                remoteTaskCompletionSource,
                runAsyncMethod,
                async (callerAppDomainId, args, taskCompletionSource, methodInfo) =>
                {
                    try
                    {
                        if (callerAppDomainId == AppDomain.CurrentDomain.Id)
                            throw new InvalidOperationException("The action is running in the default app domain instead of being run in the foreign app domain.");

                        var runner = ObjectFactory.CreateInstance<TRunner>(args);
                        var runSummary = await (Task<RunSummary>) methodInfo.Invoke(runner, null);
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
            OnAppDomainUnloading();
            _cancellationRegistration.Dispose();
            _appDomainContext.Dispose();
            OnAppDomainUnloaded();
        }

        private void OnAppDomainLoading()
        {
            _appDomainEventListener?.OnAppDomainLoading();
        }

        private void OnAppDomainLoaded()
        {
            if (_appDomainEventListener != null)
            {
                var appDomainEventListenerType = _appDomainEventListener.GetType();
                RemoteAction.Invoke(_appDomainContext.Domain,
                    appDomainEventListenerType,
                    type =>
                    {
                        var appDomainEventListener = (AppDomainEventListener)ObjectFactory.CreateInstance(type, null);
                        appDomainEventListener.OnAppDomainLoadedRemotely();
                    });
            }
        }

        private void OnAppDomainUnloading()
        {
            if (_appDomainEventListener != null)
            {
                var appDomainEventListenerType = _appDomainEventListener.GetType();
                RemoteAction.Invoke(_appDomainContext.Domain,
                    appDomainEventListenerType,
                    type =>
                    {
                        var appDomainEventListener = (AppDomainEventListener)ObjectFactory.CreateInstance(type, null);
                        appDomainEventListener.OnAppDomainUnloadingRemotely();
                    });
            }
        }

        private void OnAppDomainUnloaded()
        {
            _appDomainEventListener?.OnAppDomainUnloaded();
        }
    }
}