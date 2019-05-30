using System.Collections.Generic;
using Isolated.TestFramework;
using Isolated.TestFramework.Behaviors;
using Isolated.TestFramework.Tests;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: IsolationBehavior(IsolationLevel.Custom, typeof(TestIsolationBehavior))]

namespace Isolated.TestFramework.Tests
{
    public class TestIsolationBehavior : IIsolationBehavior
    {
        public bool IsolateTestCollection(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases)
        {
            return true;
        }
    }
}