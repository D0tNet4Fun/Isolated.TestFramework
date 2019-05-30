using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Isolated.TestFramework.Behaviors
{
    public interface IIsolationBehavior
    {
        bool IsolateTestCollection(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases);
    }
}