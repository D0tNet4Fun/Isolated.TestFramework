using System;
using System.Threading.Tasks;

namespace Isolated.TestFramework.Remoting
{
    public class RemoteTaskCompletionSource<T> : MarshalByRefObject
    {
        private readonly TaskCompletionSource<T> _taskCompletionSource = new TaskCompletionSource<T>();

        public Task<T> Task => _taskCompletionSource.Task;
        public void SetCanceled() => _taskCompletionSource.SetCanceled();
        public void SetException(Exception exception) => _taskCompletionSource.SetException(exception);
        public void SetResult(T result) => _taskCompletionSource.SetResult(result);
    }
}