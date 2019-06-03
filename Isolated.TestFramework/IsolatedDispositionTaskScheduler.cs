using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Isolated.TestFramework
{
    internal class IsolatedDispositionTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> _tasks;
        private readonly Thread _worker;

        public IsolatedDispositionTaskScheduler()
        {
            _tasks = new BlockingCollection<Task>();
            
            _worker = new Thread(ThreadBasedDispatchLoop)
            {
                IsBackground = true,
                Name = "Isolated Disposition Worker"
            };
            _worker.Start();
        }

        private void ThreadBasedDispatchLoop()
        {
            foreach (var task in _tasks.GetConsumingEnumerable())
            {
                TryExecuteTask(task);
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        public void Dispose()
        {
            try
            {
                _tasks?.CompleteAdding();
                _worker.Join();
            }
            finally
            {
                _tasks?.Dispose();
            }
        }
    }
}