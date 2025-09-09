using HttpTrafficProxy.Application.RequestHandlers.Abstractions;
using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Domain.Exceptions;
using HttpTrafficProxy.Services.Abstractions;
using HttpTrafficProxy.Services.Abstractions.Messages;

namespace HttpTrafficProxy.Application.RequestHandlers;

internal class PrimitiveProxyRequestHandler : IProxyRequestHandler
{
    private readonly IMessageKeyProvider messageKeyProvider;
    private readonly IMessagePublisher messagePublisher;
    private readonly MessageRegistry messageRegistry;

    public PrimitiveProxyRequestHandler(
        IMessageKeyProvider messageKeyProvider,
        IMessagePublisher messagePublisher,
        MessageRegistry messageRegistry)
    {
        this.messageKeyProvider = messageKeyProvider;
        this.messagePublisher = messagePublisher;
        this.messageRegistry = messageRegistry;
    }

    public async Task<HttpProxyResponse> HandleAsync(HttpProxyRequest request, CancellationToken cancellationToken)
    {
        var requestKey = messageKeyProvider.GetMessageKey(request);
        var messageData = await CreateRequestMessageContentAsync(request, cancellationToken);

        var responseTask = messageRegistry.Register(requestKey);
        await messagePublisher.PublishAsync(new MessageEnvelope(requestKey, messageData), cancellationToken);

        var responseMessage = await responseTask.WaitAsync(cancellationToken);

        return await ReadResponseMessageAsync(responseMessage, cancellationToken);
    }

    private static async Task<HttpProxyResponse> ReadResponseMessageAsync(
        MessageEnvelope response,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream(response.Data);
        using var streamReader = new StreamReader(memoryStream);

        var statusCodeLine = await streamReader.ReadLineAsync(cancellationToken);
        if (!int.TryParse(statusCodeLine, out var responseStatusCode))
        {
            throw new HttpProxyException("Не удалось прочитать статус код ответа от удаленного сервера.");
        }

        var responseBody = await streamReader.ReadToEndAsync(cancellationToken);

        return new HttpProxyResponse(responseStatusCode, responseBody);
    }

    private static async Task<byte[]> CreateRequestMessageContentAsync(
        HttpProxyRequest request,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream);

        await streamWriter.WriteLineAsync($"{request.Method} | {request.Path}");
        await streamWriter.FlushAsync(cancellationToken);

        return memoryStream.ToArray();
    }
}
