using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;

namespace Ocelot.Provider.Consul;

public static class ConsulProviderFactory
{

    private static readonly List<Consul> ServiceDiscoveryProviders = new();
    private static readonly object LockObject = new();

    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider,
        ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var factory = provider.GetService<IOcelotLoggerFactory>();
        var consulFactory = provider.GetService<IConsulClientFactory>();

        var consulRegistryConfiguration = new ConsulRegistryConfiguration(
            config.Scheme, config.Host, config.Port, route.ServiceName, config.Token);

        var pollingOptions = new ConsulPollingOptions
        {
            PollingInterval = config.PollingInterval,
            PollingType = GetPollingType(config.Type),
        };
        var consulProvider = new Consul(consulRegistryConfiguration, factory, consulFactory, pollingOptions);

        return consulProvider;

        /*lock (LockObject)
        {
            var discoveryProvider = ServiceDiscoveryProviders.FirstOrDefault(x => x.ServiceName == route.ServiceName);
            if (discoveryProvider != null)
            {
                return discoveryProvider;
            }

            discoveryProvider = new Consul(consulRegistryConfiguration, factory, consulFactory, );

            ServiceDiscoveryProviders.Add(discoveryProvider);
            return discoveryProvider;
        }*/
    }

    private static ConsulPollingType GetPollingType(string type)
    {
        if (type == Enum.GetName(typeof(ConsulPollingType), ConsulPollingType.PollConsul))
        {
            return ConsulPollingType.PollConsul;
        }
        
        return type == Enum.GetName(typeof(ConsulPollingType), ConsulPollingType.LongPolling) ? ConsulPollingType.LongPolling : ConsulPollingType.None;
    }
}
