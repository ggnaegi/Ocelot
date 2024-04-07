namespace Ocelot.Provider.Consul;

public record ConsulPollingOptions
{
    public ConsulPollingType PollingType { get; init; } = ConsulPollingType.None;
    public int PollingInterval { get; init; } = 0;
}
