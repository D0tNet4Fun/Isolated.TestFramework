using System;
using Isolated.TestFramework.Events;

namespace Isolated.TestFramework
{
    /// <summary>
    /// Configures isolation behavior for this test assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class AppDomainEventListenerAttribute : Attribute
    {
        public Type AppDomainEventListenerType { get; }

        public AppDomainEventListenerAttribute(Type appDomainEventListenerType)
        {
            if (appDomainEventListenerType == null)
                throw new ArgumentNullException(nameof(appDomainEventListenerType));
            if (!typeof(AppDomainEventListener).IsAssignableFrom(appDomainEventListenerType))
                throw new ArgumentException($"App domain event listener {appDomainEventListenerType} must derive from interface {typeof(AppDomainEventListener)}.");
            AppDomainEventListenerType = appDomainEventListenerType;
        }
    }
}