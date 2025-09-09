using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Services.Abstractions;

public interface ICoalesceKeyProvider
{
    string GetCoalesceKey(HttpProxyRequest request);
}
