using HttpTrafficProxy.Domain;
using System.Collections.Concurrent;

namespace HttpTrafficProxy.Application.RequestHandlers;

internal class AdvancedRequestCollapser
{
    private readonly ConcurrentDictionary<string, Task<HttpProxyResponse>> inflightResponseWaiters = new();

    public Task<HttpProxyResponse> RunAsync(
        string coalesceKey,
        Func<Task<HttpProxyResponse>> innerHandler,
        CancellationToken cancellationToken)
    {
        var lazy = new Lazy<Task<HttpProxyResponse>>(innerHandler);

        var responseWaiter = inflightResponseWaiters.GetOrAdd(coalesceKey, _ => lazy.Value);
        if (lazy.IsValueCreated && ReferenceEquals(lazy.Value, responseWaiter))
        {
            responseWaiter
                .ContinueWith(_ => inflightResponseWaiters.Remove(coalesceKey, out var _), cancellationToken);
        }

        return responseWaiter;
    }
}
