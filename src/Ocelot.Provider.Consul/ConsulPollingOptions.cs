namespace Ocelot.Provider.Consul;

public record ConsulPollingOptions
{
    public ConsulPollingType PollingType { get; init; }
    public int PollingInterval { get; init; }
}
