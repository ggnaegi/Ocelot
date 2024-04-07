using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Consul;

public static class ConsulProviderFactory
{
    private static readonly List<Consul> ServiceDiscoveryProviders = [];
    private static readonly object LockObject = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider,
        ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var consulFactory = provider.GetService<IConsulClientFactory>();

        var consulRegistryConfiguration = new ConsulRegistryConfiguration(
            config.Scheme, config.Host, config.Port, route.ServiceName, config.Token, config.Type,
            config.PollingInterval);

        if (consulRegistryConfiguration.PollingType() == ConsulPollingType.None)
        {
            return new Consul(consulRegistryConfiguration, factory, consulFactory);
        }

        lock (LockObject)
        {
            var discoveryProvider = ServiceDiscoveryProviders.FirstOrDefault(x => x.ServiceName == route.ServiceName);
            if (discoveryProvider != null)
            {
                return discoveryProvider;
            }

            discoveryProvider = new Consul(consulRegistryConfiguration, factory, consulFactory);
            ServiceDiscoveryProviders.Add(discoveryProvider);
            return discoveryProvider;
        }
    }
}
