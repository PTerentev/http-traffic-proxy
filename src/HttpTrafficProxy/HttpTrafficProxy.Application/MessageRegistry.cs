using HttpTrafficProxy.Domain;
using System.Collections.Concurrent;

namespace HttpTrafficProxy.Application;

internal class MessageRegistry
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<MessageEnvelope>> responseWaiters =
        new ConcurrentDictionary<string, TaskCompletionSource<MessageEnvelope>>();

    public Task<MessageEnvelope> Register(string requestKey)
    {
        var lazy = new Lazy<TaskCompletionSource<MessageEnvelope>>(() =>
            new TaskCompletionSource<MessageEnvelope>(
                TaskCreationOptions.RunContinuationsAsynchronously));

        var taskCompletionSource = responseWaiters.GetOrAdd(requestKey, _ => lazy.Value);

        if (lazy.IsValueCreated && ReferenceEquals(lazy.Value, taskCompletionSource))
        {
            taskCompletionSource.Task.ContinueWith(_ => responseWaiters.Remove(requestKey, out var _));
        }

        return taskCompletionSource.Task;
    }

    public void Complete(MessageEnvelope message)
    {
        if (responseWaiters.TryGetValue(message.RequestKey, out var responseWaiter))
        {
            responseWaiter.TrySetResult(message);
        }
    }

    public bool IsEmpty() => responseWaiters.IsEmpty;
}
