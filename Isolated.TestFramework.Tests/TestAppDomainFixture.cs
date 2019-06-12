using System;
using Isolated.TestFramework;
using Isolated.TestFramework.Tests;

[assembly: IsolatedFixture(typeof(TestAppDomainFixture))]

namespace Isolated.TestFramework.Tests
{
    public class TestAppDomainFixture : IDisposable
    {
        public TestAppDomainFixture()
        {
        }

        public void Dispose()
        {
        }
    }
}