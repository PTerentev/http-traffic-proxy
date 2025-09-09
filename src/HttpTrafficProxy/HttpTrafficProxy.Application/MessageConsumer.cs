using HttpTrafficProxy.Services.Abstractions.Messages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HttpTrafficProxy.Application;

internal class MessageConsumer : BackgroundService
{
    private readonly IMessageReader messageReader;
    private readonly MessageRegistry messageRegistry;
    private readonly ILogger<MessageConsumer> logger;

    public MessageConsumer(
        IMessageReader messageReader,
        MessageRegistry messageRegistry,
        ILogger<MessageConsumer> logger)
    {
        this.messageReader = messageReader;
        this.messageRegistry = messageRegistry;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Запущенна логика обработки входящих сообщений из брокера.");

        await foreach (var message in messageReader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            messageRegistry.Complete(message);
        }
    }
}
