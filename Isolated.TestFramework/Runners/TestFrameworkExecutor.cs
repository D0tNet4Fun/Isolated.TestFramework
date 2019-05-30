using System.Collections.Generic;
using System.Reflection;
using Isolated.TestFramework.Remoting;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Runners
{
    internal class TestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        private readonly TestCaseDeserializerArgs _args;

        public TestFrameworkExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
            _args = new TestCaseDeserializerArgs
            {
                AssemblyName = assemblyName,
                DiagnosticMessageSink = diagnosticMessageSink,
                SourceInformationProvider = sourceInformationProvider
            };
        }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using (var assemblyRunner = new TestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions, _args))
            {
                await assemblyRunner.RunAsync();
            }
        }
    }
}