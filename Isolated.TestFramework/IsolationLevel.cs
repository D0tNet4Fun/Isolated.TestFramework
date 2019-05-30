// ReSharper disable once CheckNamespace

using Isolated.TestFramework.Behaviors;

namespace Isolated.TestFramework
{
    /// <summary>
    /// Defines the level at which tests are to be run in isolation (in a separate app domain).
    /// </summary>
    public enum IsolationLevel
    {
        /// <summary>
        /// Use the current isolation enforced by the xUnit test runner. This disables the functionality of this test framework.
        /// </summary>
        Default,
        /// <summary>
        /// User defines what parts of the test assembly should run in isolation by implementing <see cref="IIsolationBehavior"/>.
        /// </summary>
        Custom,
        /// <summary>
        /// All test collections run in isolation. This ensures compatibility with all xUnit extensions and does not require user to handle cross app domain issues.
        /// </summary>
        Collections
    }
}