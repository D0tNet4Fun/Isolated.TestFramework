using System;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework
{
    internal static class ObjectFactory
    {
        public static T CreateInstance<T>(object[] args) => (T) CreateInstance(typeof(T), args);

        public static object CreateInstance(Type type, object[] args) => Activator.CreateInstance(type, args);

        public static ITestCaseOrderer CreateTestCaseOrderer(Type type, IMessageSink diagnosticMessageSink)
        {
            object instance;
            try
            {
                instance = CreateInstance(type, new object[] {diagnosticMessageSink});
            }
            catch (MissingMethodException)
            {
                instance = CreateInstance(type, null);
            }
            return (ITestCaseOrderer)instance;
        }
    }
}