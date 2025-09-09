using FluentAssertions;
using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Services.Abstractions.Messages;
using HttpTrafficProxy.Services.DependencyInjection;
using HttpTrafficProxy.Services.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace HttpTrafficProxy.Services.UnitTests;

public class FileMessageBrokerTests
{
    [Fact]
    public async Task ReadAsync_PicksUpExistingResponseFile_FromWatcherOrInitialScan()
    {
        // Arrange
        using var tmp = new TempFolder();
        var requestKey = "abc123";
        var respPath = Path.Combine(tmp.TestPath, requestKey + ".resp");
        var payload = Encoding.UTF8.GetBytes("200" + Environment.NewLine + "test");
        await File.WriteAllBytesAsync(respPath, payload);

        // Act
        await using var provider = BuildProvider(tmp.TestPath);
        var reader = provider.GetRequiredService<IMessageReader>();

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await foreach (var msg in reader.ReadAsync(cts.Token))
        {
            msg.RequestKey.Should().Be(requestKey);
            msg.Data.Should().BeEquivalentTo(payload);
            return;
        }
    }


    [Fact]
    public async Task PublishAsync_WritesRequestFile_IdempotentOnDuplicateCalls()
    {
        // Arrange
        using var tmp = new TempFolder();
        await using var provider = BuildProvider(tmp.TestPath);
        var publisher = provider.GetRequiredService<IMessagePublisher>();
        var key = "write-one";
        var data = Encoding.UTF8.GetBytes("content");

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await Task.WhenAll(
            publisher.PublishAsync(new MessageEnvelope(key, data), cts.Token),
            publisher.PublishAsync(new MessageEnvelope(key, data), cts.Token));

        // Assert
        var reqPath = Path.Combine(tmp.TestPath, key + ".req");
        File.Exists(reqPath).Should().BeTrue();

        var written = await File.ReadAllBytesAsync(reqPath, cts.Token);
        written.Should().BeEquivalentTo(data);
    }

    private static ServiceProvider BuildProvider(string testPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBroker:DirectoryPath"] = testPath
            })
            .Build();

        var services = new ServiceCollection();
        services
            .AddLogging()
            .ConfigureServices(config);

        return services.BuildServiceProvider();
    }
}
