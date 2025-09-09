using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Services.Abstractions.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace HttpTrafficProxy.Services;

internal class FileMessageBroker : IMessagePublisher, IMessageReader, IDisposable
{
    private const string RequestFileExtension = ".req";
    private const string ResponseFileExtension = ".resp";

    private readonly FileMessageBrokerOptions brokerOptions;
    private readonly ILogger<FileMessageBroker> logger;
    private bool isDisposed;
    private DirectoryInfo brokerDirectory = null!;

    private SemaphoreSlim writeSemaphore = null!;
    private FileSystemWatcher fileSystemWatcher = null!;
    private Channel<MessageEnvelope> responseMessageChannel = null!;
    private CancellationTokenSource brokerCancellationTokenSource = null!;

    public FileMessageBroker(
        ILogger<FileMessageBroker> logger,
        IOptions<FileMessageBrokerOptions> options)
    {
        this.logger = logger;
        brokerOptions = options.Value;
        InitializeMessageBroker();
    }

    public async Task PublishAsync(MessageEnvelope message, CancellationToken cancellationToken)
    {
        await writeSemaphore.WaitAsync(cancellationToken);
        try
        {
            await WriteToFileAsync(message, cancellationToken);
        }
        finally
        {
            writeSemaphore.Release();
        }
    }

    public IAsyncEnumerable<MessageEnvelope> ReadAsync(CancellationToken cancellationToken)
    {
        return responseMessageChannel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        brokerCancellationTokenSource.Dispose();

        writeSemaphore.Dispose();
        responseMessageChannel.Writer.TryComplete();
    }

    private void InitializeMessageBroker()
    {
        brokerDirectory = Directory.CreateDirectory(brokerOptions.DirectoryPath);

        brokerCancellationTokenSource = new CancellationTokenSource();

        writeSemaphore = new SemaphoreSlim(brokerOptions.ConcurrentRequestCount);
        responseMessageChannel = Channel.CreateBounded<MessageEnvelope>(
            new BoundedChannelOptions(brokerOptions.ResponseCacheCount)
            {
                SingleWriter = false,
                SingleReader = false
            });

        var responseFileFilter = $"*{ResponseFileExtension}";

        fileSystemWatcher = new FileSystemWatcher(brokerDirectory.FullName, responseFileFilter)
        {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        fileSystemWatcher.Created += (_, e) => _ = ConsumeFileAsync(e.FullPath);
        fileSystemWatcher.Changed += (_, e) => _ = ConsumeFileAsync(e.FullPath);
        fileSystemWatcher.Renamed += (_, e) => _ = ConsumeFileAsync(e.FullPath);

        foreach (var filePath in Directory.EnumerateFiles(brokerDirectory.FullName, responseFileFilter, SearchOption.TopDirectoryOnly))
        {
            var _ = ConsumeFileAsync(filePath);
        }

        RunCleaningUpExpiredFiles();

        logger.LogInformation("Инициализация брокера сообщений окончена. Путь к папке: {folder} .", brokerDirectory.FullName);
    }

    private async Task ConsumeFileAsync(string responseFilePath)
    {
        var currentRetry = 0;

        var fileName = Path.GetFileName(responseFilePath);
        var messageKey = Path.GetFileNameWithoutExtension(fileName);

        while (currentRetry <= brokerOptions.RequestRetryCount)
        {
            brokerCancellationTokenSource.Token.ThrowIfCancellationRequested();
        
            ++currentRetry;

            try
            {
                await responseMessageChannel.Writer.WaitToWriteAsync(brokerCancellationTokenSource.Token);

                if (!File.Exists(responseFilePath))
                {
                    return;
                }

                var messageData = await File.ReadAllBytesAsync(responseFilePath, brokerCancellationTokenSource.Token);

                await responseMessageChannel.Writer.WriteAsync(
                    new MessageEnvelope(messageKey, messageData),
                    brokerCancellationTokenSource.Token);

                logger.LogInformation("Файл ответа {fileName} был успешно обработан.", fileName);
            }
            catch when (currentRetry <= brokerOptions.RequestRetryCount)
            {
                await Task.Delay(brokerOptions.RequestRetryDelayMilliseconds, brokerCancellationTokenSource.Token);
                continue;
            }
            catch (Exception e)
            {
                logger.LogInformation(e, "Ошибка при обработке файла ответа {fileName}.", fileName);
            }
        
            SafeDelete(responseFilePath);
            SafeDelete(Path.Combine(brokerDirectory.FullName, $"{messageKey}{RequestFileExtension}"));

            return;
        }
    }

    private async Task WriteToFileAsync(MessageEnvelope message, CancellationToken cancellationToken)
    {
        var currentRetry = 0;

        var fileName = $"{message.RequestKey}{RequestFileExtension}";
        var filePath = Path.Combine(brokerDirectory.FullName, fileName);

        while (currentRetry <= brokerOptions.RequestRetryCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
        
            ++currentRetry;

            try
            {
                if (!File.Exists(filePath))
                {
                    await File.WriteAllBytesAsync(filePath, message.Data, cancellationToken);
                    logger.LogInformation("Файл {name} был успешно записан.", fileName);
                }
                else
                {
                    logger.LogInformation("Файл {name} уже существует в папке брокера.", fileName);
                }
            }
            catch when (currentRetry <= brokerOptions.RequestRetryCount)
            {
                await Task.Delay(brokerOptions.RequestRetryDelayMilliseconds, cancellationToken);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при записи файла {name} .", fileName);
            }

            return;
        }
    }

    private void RunCleaningUpExpiredFiles()
    {
        var fileTimeToLive = TimeSpan.FromMinutes(brokerOptions.FileTimeToLiveMinutes);

        Task.Run(
            async () =>
            {
                while (!brokerCancellationTokenSource.IsCancellationRequested)
                {
                    var utcNow = DateTimeOffset.UtcNow;

                    try
                    {
                        var filePathsToCheck = Directory.EnumerateFiles(brokerDirectory.FullName, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => f.EndsWith(RequestFileExtension, StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(ResponseFileExtension, StringComparison.OrdinalIgnoreCase));

                        foreach (var filePath in filePathsToCheck.ToArray())
                        {
                            var age = utcNow - File.GetCreationTimeUtc(filePath);
                            if (age > fileTimeToLive)
                            {
                                SafeDelete(filePath);
                            }
                        }

                        logger.LogInformation("Очистка файлов брокера успешно завершена.");
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Ошибка при очистке файлов брокера.");
                    }

                    await Task.Delay(fileTimeToLive / 2, brokerCancellationTokenSource.Token);
                }
            });
    }

    private void SafeDelete(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                logger.LogInformation("Файла {name} был успешно удален.", fileName);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении файла {name}.", fileName);
        }
    }
}
