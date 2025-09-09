using HttpTrafficProxy.Application.RequestHandlers.Abstractions;
using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Services.Abstractions;

namespace HttpTrafficProxy.Application.RequestHandlers;

internal class AdvancedProxyRequestHandler : IProxyRequestHandler
{
    private readonly IProxyRequestHandler requestHandler;
    private readonly ICoalesceKeyProvider coalesceKeyProvider;
    private readonly AdvancedRequestCollapser advancedRequestCollapser;

    public AdvancedProxyRequestHandler(
        IProxyRequestHandler requestHandler,
        ICoalesceKeyProvider coalesceKeyProvider,
        AdvancedRequestCollapser advancedRequestCollapser)
    {
        this.requestHandler = requestHandler;
        this.coalesceKeyProvider = coalesceKeyProvider;
        this.advancedRequestCollapser = advancedRequestCollapser;
    }

    public async Task<HttpProxyResponse> HandleAsync(HttpProxyRequest request, CancellationToken cancellationToken)
    {
        var coalesceKey = coalesceKeyProvider.GetCoalesceKey(request);
        return await advancedRequestCollapser.RunAsync(
            coalesceKey,
            () => requestHandler.HandleAsync(request, CancellationToken.None),
            cancellationToken);
    }
}
