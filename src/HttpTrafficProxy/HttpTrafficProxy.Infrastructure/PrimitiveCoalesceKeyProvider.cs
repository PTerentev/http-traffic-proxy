using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Services.Abstractions;

namespace HttpTrafficProxy.Services;

internal class PrimitiveCoalesceKeyProvider : ICoalesceKeyProvider
{
    public string GetCoalesceKey(HttpProxyRequest request)
    {
        return $"{request.Method}|{request.Path}".ToLowerInvariant();
    }
}
