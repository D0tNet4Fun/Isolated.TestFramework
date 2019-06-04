using System;
using Xunit.Sdk;

namespace Isolated.TestFramework.Remoting
{
    [Serializable]
    internal class SerializableRunSummary
    {
        public SerializableRunSummary(RunSummary runSummary)
        {
            Total = runSummary.Total;
            Failed = runSummary.Failed;
            Skipped = runSummary.Skipped;
            Time = runSummary.Time;
        }

        public int Total { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public decimal Time { get; set; }

        public RunSummary AsRunSummary() => new RunSummary
        {
            Total = Total,
            Failed = Failed,
            Skipped = Skipped,
            Time = Time
        };
    }
}