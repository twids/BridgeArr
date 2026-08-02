using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace BridgeArr.Api;

/// <summary>
/// Registers API controllers and serialization settings.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBridgeArrApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });

        services.AddEndpointsApiExplorer();
        return services;
    }
}
