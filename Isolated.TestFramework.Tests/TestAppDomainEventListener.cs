using Isolated.TestFramework;
using Isolated.TestFramework.Events;
using Isolated.TestFramework.Tests;

[assembly: AppDomainEventListener(typeof(TestAppDomainEventListener))]

namespace Isolated.TestFramework.Tests
{
    public class TestAppDomainEventListener : AppDomainEventListener
    {
        public TestAppDomainEventListener()
        {
        }

        public override void OnAppDomainLoadedRemotely()
        {
            base.OnAppDomainLoadedRemotely();
        }
    }
}