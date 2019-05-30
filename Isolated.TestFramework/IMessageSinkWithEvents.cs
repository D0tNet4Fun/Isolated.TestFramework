using System;
using Xunit.Abstractions;

namespace Isolated.TestFramework
{
    public interface IMessageSinkWithEvents : IMessageSink
    {
        event EventHandler<ITestCollection> TestCollectionFinished;
    }
}