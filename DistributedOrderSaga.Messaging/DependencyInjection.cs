using Microsoft.Extensions.DependencyInjection;

namespace DistributedOrderSaga.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMQExtensions(
        this IServiceCollection services)
    {
        services.AddSingleton<Publisher>();
        services.AddSingleton<BaseMessageConsumer>(); 
        
        return services;
    }
}