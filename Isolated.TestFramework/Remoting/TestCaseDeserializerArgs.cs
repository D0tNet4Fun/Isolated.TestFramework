using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Isolated.TestFramework.Remoting
{
    internal class TestCaseDeserializerArgs : MarshalByRefObject
    {
        public AssemblyName AssemblyName { get; set; }
        public ISourceInformationProvider SourceInformationProvider { get; set; }
        public IMessageSink DiagnosticMessageSink { get; set; }
    }
}