using HttpTrafficProxy.Application.RequestHandlers;
using HttpTrafficProxy.Application.RequestHandlers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HttpTrafficProxy.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ApplicationOptions>()
            .Bind(configuration.GetSection("Application"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<MessageRegistry>();
        services.AddHostedService<MessageConsumer>();

        services.AddSingleton<AdvancedRequestCollapser>();

        services.AddScoped(provider =>
        {
            IProxyRequestHandler handler = ActivatorUtilities.CreateInstance<PrimitiveProxyRequestHandler>(provider);

            var useAdvancedMode = provider.GetRequiredService<IOptions<ApplicationOptions>>().Value.UseAdvancedMode;
            if (useAdvancedMode)
            {
                handler = ActivatorUtilities.CreateInstance<AdvancedProxyRequestHandler>(provider, handler);
            }

            return handler;
        });

        return services;
    }
}
