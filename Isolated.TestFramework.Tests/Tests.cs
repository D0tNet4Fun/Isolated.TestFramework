using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Isolated.TestFramework.IsolatedTestFramework", "Isolated.TestFramework")]
//[assembly: TestCaseOrderer("Isolated.TestFramework.Tests.Tests+MyTestCaseOrderer", "Isolated.TestFramework.Tests")]

namespace Isolated.TestFramework.Tests
{
    [TestCaseOrderer("Isolated.TestFramework.Tests.Tests+MyTestCaseOrderer", "Isolated.TestFramework.Tests")]
    public class Tests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public Tests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Hello()
        {
            var message = $"Hello from app domain {AppDomain.CurrentDomain.Id}!";
            _testOutputHelper.WriteLine(message);
            Console.WriteLine(message);  // shows in the test runner console
        }

        [Fact]
        public void Throw()
        {
            var message = $"Hello from app domain {AppDomain.CurrentDomain.Id}!";
            _testOutputHelper.WriteLine(message);
            Console.WriteLine(message);  // shows in the test runner console
            throw new Exception("This is expected to fail");
        }

        public class MyTestCaseOrderer : ITestCaseOrderer
        {
            public MyTestCaseOrderer()
            {
            }

            public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
            {
                return testCases.OrderBy(t => t.TestMethod.Method.Name).ToArray();
            }
        }
    }
}
