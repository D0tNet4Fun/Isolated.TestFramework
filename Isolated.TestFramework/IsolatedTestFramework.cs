using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;
using Isolated.TestFramework.Runners;

namespace Isolated.TestFramework
{
    public class IsolatedTestFramework : XunitTestFramework
    {
        public IsolatedTestFramework(IMessageSink messageSink)
            : base(messageSink)
        {
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new TestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }
    }
}