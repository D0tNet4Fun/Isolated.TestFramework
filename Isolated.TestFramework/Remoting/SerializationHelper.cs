using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Isolated.TestFramework.Remoting
{
    internal static class SerializationHelper
    {
        private static readonly Type SerializationInfoType = Type.GetType("Xunit.Sdk.SerializationHelper, xunit.execution.desktop");
        private static readonly MethodInfo SerializeMethod = SerializationInfoType.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static);

        public static string Serialize(ITestCase testCase)
        {
            return (string)SerializeMethod.Invoke(null, new object[] { testCase });
        }
    }
}