using System;
using Xunit.Sdk;

namespace Isolated.TestFramework.Remoting
{
    [Serializable]
    internal class SerializableRunSummary
    {
        /// <summary>The total number of tests run.</summary>
        public int Total;
        /// <summary>The number of failed tests.</summary>
        public int Failed;
        /// <summary>The number of skipped tests.</summary>
        public int Skipped;
        /// <summary>The total time taken to run the tests, in seconds.</summary>
        public decimal Time;

        public SerializableRunSummary(RunSummary runSummary)
        {
            Total = runSummary.Total;
            Failed = runSummary.Failed;
            Skipped = runSummary.Skipped;
            Time = runSummary.Time;
        }

        public RunSummary Deserialize() => new RunSummary
        {
            Total = Total,
            Failed = Failed,
            Skipped = Skipped,
            Time = Time
        };
    }
}