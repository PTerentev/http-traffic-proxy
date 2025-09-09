using HttpTrafficProxy.Services.Abstractions;
using HttpTrafficProxy.Services.Abstractions.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HttpTrafficProxy.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMessageKeyProvider, Md5MessageKeyProvider>();
        services.AddScoped<ICoalesceKeyProvider, PrimitiveCoalesceKeyProvider>();

        services
            .AddOptions<FileMessageBrokerOptions>()
            .Bind(configuration.GetSection("MessageBroker"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<FileMessageBroker>();
        services.AddSingleton<IMessagePublisher>(provider => provider.GetRequiredService<FileMessageBroker>());
        services.AddSingleton<IMessageReader>(provider => provider.GetRequiredService<FileMessageBroker>());

        return services;
    }
}
