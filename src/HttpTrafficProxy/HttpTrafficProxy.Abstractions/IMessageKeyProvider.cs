using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Services.Abstractions;

public interface IMessageKeyProvider
{
    string GetMessageKey(HttpProxyRequest request);
}
