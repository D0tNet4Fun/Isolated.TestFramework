using System;
using System.Threading.Tasks;
using Xunit;

namespace Isolated.TestFramework.Remoting
{
    public class RemoteTaskCompletionSource<T> : LongLivedMarshalByRefObject
    {
        private readonly TaskCompletionSource<T> _taskCompletionSource = new TaskCompletionSource<T>();

        public Task<T> Task => _taskCompletionSource.Task;
        public void SetCanceled() => _taskCompletionSource.SetCanceled();
        public void SetException(Exception exception) => _taskCompletionSource.SetException(exception);
        public void SetResult(T result) => _taskCompletionSource.SetResult(result);
    }
}