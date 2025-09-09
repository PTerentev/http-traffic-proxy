using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Application.RequestHandlers.Abstractions;

public interface IProxyRequestHandler
{
    Task<HttpProxyResponse> HandleAsync(HttpProxyRequest request, CancellationToken cancellationToken);
}
