using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.ServiceDiscovery
{
    public class ServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
    {
        private readonly IServiceProvider _provider;
        private readonly ServiceDiscoveryFinderDelegate _delegates;
        private readonly IOcelotLogger _logger;

        // TODO: This should be refactored, the service discovery providers types are not known upfront.
        public const string ServiceFabric = "ServiceFabric";
        public const string Consul = "Consul";
        public const string PollConsul = "PollConsul";
        public const string LongPolling = "LongPolling";

        public ServiceDiscoveryProviderFactory(IOcelotLoggerFactory factory, IServiceProvider provider)
        {
            _provider = provider;
            _delegates = provider.GetService<ServiceDiscoveryFinderDelegate>();
            _logger = factory.CreateLogger<ServiceDiscoveryProviderFactory>();
        }

        public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
        {
            if (route.UseServiceDiscovery)
            {
                var routeName = route.UpstreamPathTemplate?.Template ?? route.ServiceName ?? string.Empty;
                _logger.LogInformation(() => $"The {nameof(DownstreamRoute.UseServiceDiscovery)} mode of the route '{routeName}' is enabled.");
                return GetServiceDiscoveryProvider(serviceConfig, route);
            }

            var services = route.DownstreamAddresses
                .Select(address => new Service(
                    route.ServiceName,
                    new ServiceHostAndPort(address.Host, address.Port, route.DownstreamScheme),
                    string.Empty,
                    string.Empty,
                    Enumerable.Empty<string>()))
                .ToList();

            return new OkResponse<IServiceDiscoveryProvider>(new ConfigurationServiceProvider(services));
        }

        private Response<IServiceDiscoveryProvider> GetServiceDiscoveryProvider(ServiceProviderConfiguration config, DownstreamRoute route)
        {
            _logger.LogInformation(() => $"Getting service discovery provider of {nameof(config.Type)} '{config.Type}'...");

            if (config.Type != null && config.Type.Equals(ServiceFabric, StringComparison.OrdinalIgnoreCase))
            {
                var sfConfig = new ServiceFabricConfiguration(config.Host, config.Port, route.ServiceName);
                return new OkResponse<IServiceDiscoveryProvider>(new ServiceFabricServiceDiscoveryProvider(sfConfig));
            }

            if (_delegates != null)
            {
                var provider = _delegates?.Invoke(_provider, config, route);
                if (string.Equals(provider?.GetType().Name, config.Type, StringComparison.OrdinalIgnoreCase) || IsConsulProvider(provider, config))
                {
                    return new OkResponse<IServiceDiscoveryProvider>(provider);
                }
            }

            var message = $"Unable to find service discovery provider for {nameof(config.Type)}: '{config.Type}'!";
            _logger.LogWarning(() => $"Unable to find service discovery provider for {nameof(config.Type)}: '{config.Type}'!");
            return new ErrorResponse<IServiceDiscoveryProvider>(new UnableToFindServiceDiscoveryProviderError(message));
        }

        /// <summary>
        /// Method to verify if the provider is consul and if the polling type is known.
        /// TODO: this should be refactored in the future, probably by adding a method to IServiceDiscoveryProvider.
        /// </summary>
        /// <param name="provider">The provider that should be verified.</param>
        /// <param name="config">The service provider configuration.</param>
        /// <returns>True if the provider is consul and polling type has been verified.</returns>
        private static bool IsConsulProvider(IServiceDiscoveryProvider provider, ServiceProviderConfiguration config)
        {
            var isConsulProvider = string.Equals(provider?.GetType().Name, Consul, StringComparison.OrdinalIgnoreCase);
            var typeMatchesConfig = config.Type != null && (
                config.Type.Equals(Consul, StringComparison.OrdinalIgnoreCase) ||
                config.Type.Equals(PollConsul, StringComparison.OrdinalIgnoreCase) ||
                config.Type.Equals(LongPolling, StringComparison.OrdinalIgnoreCase));

            return isConsulProvider && typeMatchesConfig;
        }
    }
}
