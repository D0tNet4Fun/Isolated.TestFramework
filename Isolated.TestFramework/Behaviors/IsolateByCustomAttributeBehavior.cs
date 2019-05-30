using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Behaviors
{
    /// <summary>
    /// Enable isolation for classes and/or methods decorated with a specific attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
    public class IsolateByCustomAttributeBehavior<TAttribute> : IIsolationBehavior
        where TAttribute : Attribute
    {
        public bool IsolateTestCollection(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases)
        {
            var groups = testCases.GroupBy(testCase => testCase.TestMethod.TestClass);
            foreach (var group in groups)
            {
                if (HasMatchingAttribute(group.Key.Class)) return true;
                if (group.Any(testCase => HasMatchingAttribute(testCase.Method))) return true;
            }

            return false;
        }

        private bool HasMatchingAttribute(ITypeInfo testClass)
        {
            return ContainsMatchingAttribute(testClass.GetCustomAttributes(typeof(TAttribute)));
        }

        private bool HasMatchingAttribute(IMethodInfo testMethod)
        {
            return ContainsMatchingAttribute(testMethod.GetCustomAttributes(typeof(TAttribute)));
        }

        private bool ContainsMatchingAttribute(IEnumerable<IAttributeInfo> customAttributes)
        {
            return customAttributes.Any(IsMatchingAttribute);
        }

        /// <summary>
        /// When overriden in derived classes, this can be used to analyze the state of the custom attribute. Returns <c>true</c> unless overridden.
        /// </summary>
        /// <param name="customAttribute">The custom attribute of type <see cref="TAttribute"/>.</param>
        protected virtual bool IsMatchingAttribute(IAttributeInfo customAttribute)
        {
            return true;
        }
    }
}