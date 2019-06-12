using System;

namespace Isolated.TestFramework
{
    /// <summary>
    /// Configures isolated fixtures for this test assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IsolatedFixtureAttribute : Attribute
    {
        public Type[] FixtureTypes { get; set; }

        public IsolatedFixtureAttribute(params Type[] fixtureTypes)
        {
            FixtureTypes = fixtureTypes;
        }
    }
}