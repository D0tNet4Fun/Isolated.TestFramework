using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Isolated.TestFramework.Remoting
{
    [Serializable]
    internal class TestCaseDeserializerArgs
    {
        public AssemblyName AssemblyName { get; set; }
        public ISourceInformationProvider SourceInformationProvider { get; set; }
        public IMessageSink DiagnosticMessageSink { get; set; }
    }
}