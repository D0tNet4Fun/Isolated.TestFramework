using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Behaviors
{
    public class IsolateTestCollectionBehavior : IIsolationBehavior
    {
        public bool IsolateTestCollection(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases)
        {
            return true;
        }
    }
}