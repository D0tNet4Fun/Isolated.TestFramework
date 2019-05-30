using System;
using AppDomainToolkit;
using Isolated.TestFramework.Remoting;
using Isolated.TestFramework.Runners;
using Xunit.Abstractions;

namespace Isolated.TestFramework
{
    internal class RemoteObjectFactory
    {
        private readonly AppDomain _appDomain;
        private readonly TestCaseDeserializerArgs _testCaseDeserializerArgs;
        private readonly Lazy<TestCaseDeserializer> _lazyRemoteTestCaseDeserializer;

        public RemoteObjectFactory(AppDomain appDomain, TestCaseDeserializerArgs testCaseDeserializerArgs)
        {
            _appDomain = appDomain;
            _testCaseDeserializerArgs = testCaseDeserializerArgs;
            _lazyRemoteTestCaseDeserializer = new Lazy<TestCaseDeserializer>(CreateTestCaseDeserializer);
        }

        private TestCaseDeserializer CreateTestCaseDeserializer()
        {
            return RemoteFunc.Invoke(
                _appDomain,
                _testCaseDeserializerArgs,
                args => new TestCaseDeserializer(args.AssemblyName, args.SourceInformationProvider, args.DiagnosticMessageSink)
            );
        }

        public ITestCase CreateTestCaseFrom(ITestCase testCase)
        {
            // serialize the test case in the caller's app domain (the default app domain)
            // then deserialize it in our app domain

            var serializedTestCase = SerializationHelper.Serialize(testCase);
            return RemoteFunc.Invoke(_appDomain,
                serializedTestCase,
                _lazyRemoteTestCaseDeserializer,
                (data, deserializer) =>
                {
                    var remoteDeserializer = deserializer.Value;
                    return remoteDeserializer.Deserialize(data);
                });
        }
    }
}