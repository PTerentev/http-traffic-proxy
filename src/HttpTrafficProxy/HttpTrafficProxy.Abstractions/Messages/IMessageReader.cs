using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Services.Abstractions.Messages;

public interface IMessageReader
{
    IAsyncEnumerable<MessageEnvelope> ReadAsync(CancellationToken cancellationToken);
}
