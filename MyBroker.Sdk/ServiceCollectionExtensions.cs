using Microsoft.Extensions.DependencyInjection;

namespace MyBroker.Sdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyBrokerSdk(this IServiceCollection services, string baseAddress)
    {
        services.AddHttpClient<IBrokerApiClient, BrokerApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseAddress.TrimEnd('/') + '/');
        });

        return services;
    }
}
