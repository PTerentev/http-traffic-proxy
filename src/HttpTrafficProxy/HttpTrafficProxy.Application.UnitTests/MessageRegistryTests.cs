using FluentAssertions;
using HttpTrafficProxy.Domain;
using System.Text;

namespace HttpTrafficProxy.Application.UnitTests;

public class MessageRegistryTests
{
    [Fact]
    public async Task Register_TwoClientsSameKey_GetsSameResult()
    {
        // Arrange
        var registry = new MessageRegistry();
        var key = "req-1";

        // Act
        var t1 = registry.Register(key);
        var t2 = registry.Register(key);

        registry.Complete(new MessageEnvelope(key, Encoding.UTF8.GetBytes("resp")));

        var msg1 = await t1;
        var msg2 = await t2;

        // Assert
        msg1.Should().Be(msg2);

        msg2.RequestKey.Should().Be(key);
        msg2.Data.Should().NotBeNull();
    }
}
