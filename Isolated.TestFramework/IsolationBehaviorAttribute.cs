using System;
using Isolated.TestFramework.Behaviors;

// ReSharper disable once CheckNamespace
namespace Isolated.TestFramework
{
    /// <summary>
    /// Configures isolation behavior for this test assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class IsolationBehaviorAttribute : Attribute
    {
        public IsolationLevel IsolationLevel { get; }

        public Type IsolationBehaviorType { get; }

        public IsolationBehaviorAttribute(IsolationLevel isolationLevel, Type isolationBehaviorType = null)
        {
            if (IsolationLevel == IsolationLevel.Custom)
            {
                if (isolationBehaviorType == null)
                    throw new ArgumentException("Isolation behavior type must be specified when the isolation level is custom.", nameof(isolationBehaviorType));
                if (!typeof(IIsolationBehavior).IsAssignableFrom(isolationBehaviorType))
                    throw new ArgumentException($"Isolation behavior {isolationBehaviorType} must implement interface {typeof(IIsolationBehavior)}.");
            }

            IsolationLevel = isolationLevel;
            IsolationBehaviorType = isolationBehaviorType;
        }
    }
}