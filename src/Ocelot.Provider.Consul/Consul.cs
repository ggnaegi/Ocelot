using System.Collections.Immutable;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Threading;

namespace Ocelot.Provider.Consul;

public class Consul : IServiceDiscoveryProvider
{
    private const string VersionPrefix = "version-";
    private readonly ConsulRegistryConfiguration _config;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private ImmutableList<Service> _services;
    private readonly ConsulPollingOptions _consulProviderOptions;

    public Consul(ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory, ConsulPollingOptions consulProviderOptions)
    {
        _config = config;
        _consul = clientFactory.Get(_config);
        _logger = factory.CreateLogger<Consul>();
        _consulProviderOptions = consulProviderOptions;
    }

    public async Task<List<Service>> GetAsync()
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (_services != null)
            {
                return[.. _services];
            }

            (_services, var initialWaitIndex) = await RetrieveServiceListFromConsulAsync(0);

            ConfigurePollingType(initialWaitIndex);
            
            return[.. _services];
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public string ServiceName => _config.KeyOfServiceInConsul;

    private void ConfigurePollingType(ulong initialWaitIndex)
    {
        switch (_consulProviderOptions.PollingType)
        {
            case ConsulPollingType.None:
                return;
            case ConsulPollingType.LongPolling:
                _ = LongPollingServiceListAsync(initialWaitIndex);
                return;
            case ConsulPollingType.PollConsul:
            default:
                _ = new Timer(_ =>
                {
                    (_services, var waitIndex) = RetrieveServiceListFromConsulAsync(0).Result;
                }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_consulProviderOptions.PollingInterval));
                break;
        }
    }

    private async Task LongPollingServiceListAsync(ulong initialWaitIndex)
    {
        var waitIndex = initialWaitIndex;
        while (true)
        {
            try
            {
                (_services, waitIndex) = await RetrieveServiceListFromConsulAsync(waitIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(() => $"Error in long polling: {ex.Message}", ex);
            }
        }
    }

    public virtual async Task<(ImmutableList<Service> ServiceList, ulong WaitIndex)> RetrieveServiceListFromConsulAsync(ulong waitIndex)
    {
        var queryResult = await _consul.Health.Service(_config.KeyOfServiceInConsul, string.Empty, true, new QueryOptions
        {
            WaitIndex = waitIndex,
        });

        var services = new List<Service>();
        foreach (var serviceEntry in queryResult.Response)
        {
            var service = serviceEntry.Service;
            if (IsValid(service))
            {
                var nodes = await _consul.Catalog.Nodes();
                if (nodes.Response == null)
                {
                    services.Add(BuildService(serviceEntry, null));
                }
                else
                {
                    var serviceNode = nodes.Response.FirstOrDefault(n => n.Address == service.Address);
                    services.Add(BuildService(serviceEntry, serviceNode));
                }
            }
            else
            {
                _logger.LogWarning(
                    () => $"Unable to use service address: '{service.Address}' and port: {service.Port} as it is invalid for the service: '{service.Service}'. Address must contain host only e.g. 'localhost', and port must be greater than 0.");
            }
        }

        return (services.ToImmutableList(), queryResult.LastIndex);
    }

    private static Service BuildService(ServiceEntry serviceEntry, Node serviceNode)
    {
        var service = serviceEntry.Service;
        return new Service(
            service.Service,
            new ServiceHostAndPort(
                serviceNode == null ? service.Address : serviceNode.Name,
                service.Port),
            service.ID,
            GetVersionFromStrings(service.Tags),
            service.Tags ?? Enumerable.Empty<string>());
    }

    private static bool IsValid(AgentService service)
        => !string.IsNullOrEmpty(service.Address)
        && !service.Address.Contains($"{Uri.UriSchemeHttp}://")
        && !service.Address.Contains($"{Uri.UriSchemeHttps}://")
        && service.Port > 0;

    private static string GetVersionFromStrings(IEnumerable<string> strings)
        => strings?.FirstOrDefault(x => x.StartsWith(VersionPrefix, StringComparison.Ordinal))
            .TrimStart(VersionPrefix);
}
