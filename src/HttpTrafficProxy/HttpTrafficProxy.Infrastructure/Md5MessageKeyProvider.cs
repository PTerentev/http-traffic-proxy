using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Services.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace HttpTrafficProxy.Services;

internal class Md5MessageKeyProvider : IMessageKeyProvider
{
    public string GetMessageKey(HttpProxyRequest request)
    {
        var requestBytes = Encoding.UTF8.GetBytes(request.Method + request.Path);
        var hashBytes = MD5.HashData(requestBytes);
        return Convert.ToHexString(hashBytes);
    }
}
