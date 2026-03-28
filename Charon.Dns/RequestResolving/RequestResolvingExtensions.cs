using Charon.Dns.RequestResolving.ResolvingStrategies;
using Charon.Dns.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Charon.Dns.RequestResolving;

public static class RequestResolvingExtensions
{
    extension (IServiceCollection services)
    {
        public IServiceCollection AddRequestResolving()
        {
            const string strategyCollection = "resolving-strategy-collection";
            services
                .AddSingleton<ISmartRequestResolver, SmartRequestResolver>()
                .AddSingleton<IDefaultRequestResolver, DefaultRequestResolver>()
                .AddSingleton<ISafeRequestResolver, SafeRequestResolver>()
                .AddKeyedSingleton<IResolvingStrategy, RoundRobinResolvingStrategy>(strategyCollection)
                .AddKeyedSingleton<IResolvingStrategy, RandomResolvingStrategy>(strategyCollection)
                .AddKeyedSingleton<IResolvingStrategy, ParallelResolvingStrategy>(strategyCollection)
                .AddTransient(serviceProvider =>
                {
                    var dnsChainSettings = serviceProvider.GetRequiredService<DnsChainSettings>();
                    var resolvingStrategies = serviceProvider.GetKeyedServices<IResolvingStrategy>(strategyCollection);
                    return resolvingStrategies.Single(x => x.Strategy == dnsChainSettings.ResolvingStrategy);
                });
            
            return services;
        }
    }
}
