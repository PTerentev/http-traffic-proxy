using HttpTrafficProxy.Domain;
using System.Collections.Concurrent;

namespace HttpTrafficProxy.Application.RequestHandlers;

internal class AdvancedRequestCollapser
{
    private readonly ConcurrentDictionary<string, Task<HttpProxyResponse>> inflightResponseWaiters = new();

    public async Task<HttpProxyResponse> RunAsync(
        string coalesceKey,
        Func<Task<HttpProxyResponse>> innerHandler,
        CancellationToken cancellationToken)
    {
        var task = inflightResponseWaiters.GetOrAdd(coalesceKey, _ => innerHandler());

        try
        {
            return await task.WaitAsync(cancellationToken);
        }
        finally
        {
            inflightResponseWaiters
                .TryRemove(new KeyValuePair<string, Task<HttpProxyResponse>>(coalesceKey, task));
        }
    }
}
