using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Services.Abstractions.Messages;

public interface IMessagePublisher
{
    Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken);
}
