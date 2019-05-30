using System;
using Xunit;
using Xunit.Abstractions;

[assembly: TestFramework("Isolated.TestFramework.IsolatedTestFramework", "Isolated.TestFramework")]

namespace Isolated.TestFramework.Tests
{
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
    }
}
