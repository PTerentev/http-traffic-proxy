using FluentAssertions;
using HttpTrafficProxy.Application.RequestHandlers;
using HttpTrafficProxy.Domain;

namespace HttpTrafficProxy.Application.UnitTests;

public class AdvancedRequestCollapserTests
{
    [Fact]
    public async Task RunAsync_SameKey_Coalesces_AllCallersGetSameResult_ThenCleansUp()
    {
        // Arrange
        var collapser = new AdvancedRequestCollapser();
        var calls = 0;

        async Task<HttpProxyResponse> Inner()
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50);
            return new HttpProxyResponse(200, "ok");
        }

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => collapser.RunAsync("KEY", Inner, cts.Token))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        calls.Should().Be(1);
        results.Should().OnlyContain(r => r.StatusCode == 200 && r.Body == "ok");

        var res2 = await collapser.RunAsync("KEY", Inner, cts.Token);
        calls.Should().Be(2);
        res2.StatusCode.Should().Be(200);
    }
}
