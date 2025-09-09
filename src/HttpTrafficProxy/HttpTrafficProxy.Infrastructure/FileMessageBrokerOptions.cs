using System.ComponentModel.DataAnnotations;

namespace HttpTrafficProxy.Services;

public record FileMessageBrokerOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string DirectoryPath { get; init; }

    [Range(0, 20)]
    public required int RequestRetryCount { get; init; } = 5;

    [Range(0, 40000)]
    public required int RequestRetryDelayMilliseconds { get; init; } = 25;

    [Range(1, 256)]
    public required int ConcurrentRequestCount { get; init; } = 128;

    [Range(1, 256)]
    public required int ResponseCacheCount { get; init; } = 128;

    [Range(1, 500)]
    public required int FileTimeToLiveMinutes { get; init; } = 5;
}
