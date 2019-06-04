using System;
using Xunit.Abstractions;

namespace Isolated.TestFramework
{
    internal interface IMessageSinkWithEvents : IMessageSink
    {
        event EventHandler<ITestCollection> TestCollectionFinished;
    }
}