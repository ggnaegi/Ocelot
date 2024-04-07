using System.Collections.Immutable;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Threading;

namespace Ocelot.Provider.Consul;

public class Consul : IServiceDiscoveryProvider, IDisposable
{
    private const string VersionPrefix = "version-";
    private readonly ConsulRegistryConfiguration _config;
    private readonly IConsulClient _consul;
    private readonly IOcelotLogger _logger;
    private ImmutableList<Service> _services;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private CancellationTokenSource _cancellationTokenSource;

    public Consul(ConsulRegistryConfiguration config, IOcelotLoggerFactory factory, IConsulClientFactory clientFactory)
    {
        _config = config;
        _consul = clientFactory.Get(_config);
        _logger = factory.CreateLogger<Consul>();
    }

    public async Task<List<Service>> GetAsync()
    {
        // getting the services using the polling and long polling mechanisms
        if (_config.PollingType() != ConsulPollingType.None)
        {
            return await GetFromPollingAsync();
        }

        // if no polling, get the services from consul directly
        var (services, _) = await RetrieveServiceListFromConsulAsync(0);
        return [.. services];
    }

    public string ServiceName => _config.KeyOfServiceInConsul;

    /// <summary>
    /// Retrieving the services list from consul using polling.
    /// It can be done using either polling or long polling.
    /// If polling is enabled, it will poll the services list from consul at a regular interval.
    /// If long polling is enabled, it will poll the services list from consul and wait for the next change.
    /// The timeout of the long polling is configured in consul, the wait time is 5 minutes by default.
    /// </summary>
    /// <returns>The services list.</returns>
    private async Task<List<Service>> GetFromPollingAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_services != null)
            {
                return [.. _services];
            }

            (_services, var initialWaitIndex) = await RetrieveServiceListFromConsulAsync(0);

            ConfigurePollingType(initialWaitIndex);

            return [.. _services];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Configure the polling type.
    /// </summary>
    /// <param name="initialWaitIndex">This is a key returned by consul and used for the long polling.</param>
    private void ConfigurePollingType(ulong initialWaitIndex)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        Task.Run(() => PollingServicesListAsync(initialWaitIndex, 
            _config.PollingType() == ConsulPollingType.PollConsul ? _config.PollingInterval: null, cancellationToken), 
            cancellationToken);
    }

    /// <summary>
    /// Polling the services list from consul.
    /// If the polling interval is not null, it will poll the services list at a regular interval,
    /// otherwise it will poll the services list and wait for the next change.
    /// </summary>
    /// <param name="initialWaitIndex">This is a key returned by consul and used for the long polling.</param>
    /// <param name="pollingInterval">The polling interval in ms.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task PollingServicesListAsync(ulong initialWaitIndex, int? pollingInterval, CancellationToken cancellationToken)
    {
        var waitIndex = initialWaitIndex;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (pollingInterval.HasValue)
                {
                    await Task.Delay(pollingInterval.Value, cancellationToken);
                }

                (_services, waitIndex) = await RetrieveServiceListFromConsulAsync(pollingInterval.HasValue ? 0 : waitIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(() => $"Error in polling: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Retrieve the services list from consul.
    /// If the waitIndex is 0, it will retrieve the services list without waiting for the next change.
    /// </summary>
    /// <param name="waitIndex">The current key returned by consul for the long polling.</param>
    /// <returns>The current service list and the updated wait index.</returns>
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _consul?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
