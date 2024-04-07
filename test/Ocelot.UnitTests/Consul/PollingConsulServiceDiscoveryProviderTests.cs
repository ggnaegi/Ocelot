using System.Collections.Immutable;
using Consul;
using Moq;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace Ocelot.UnitTests.Consul
{
    public class PollingConsulServiceDiscoveryProviderTests
    {
        private readonly int _delay;
        private readonly List<Service> _services;
        private readonly Mock<IOcelotLoggerFactory> _factory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly Mock<Provider.Consul.Consul> _consulServiceDiscoveryProvider;
        private List<Service> _result;

        public PollingConsulServiceDiscoveryProviderTests()
        {
            _services = new List<Service>();
            _delay = 1;
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<Provider.Consul.Consul>()).Returns(_logger.Object);

            var consulClientFactory = new Mock<IConsulClientFactory>();
            var consulClient = new Mock<IConsulClient>();
            consulClientFactory.Setup(x => x.Get(It.IsAny<ConsulRegistryConfiguration>())).Returns(consulClient.Object);
            _consulServiceDiscoveryProvider = new Mock<Provider.Consul.Consul>(new ConsulRegistryConfiguration("http", "localhost", 80, "test", null, "PollConsul", _delay), _factory.Object, consulClientFactory.Object) { CallBase = true };
        }

        [Fact]
        public void should_return_service_from_consul()
        {
            var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());

            this.Given(x => GivenConsulReturns(service))
                .When(x => WhenIGetTheServices(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        [Fact]
        public void should_return_service_from_consul_without_delay()
        {
            var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());

            this.Given(x => GivenConsulReturns(service))
                .When(x => WhenIGetTheServicesWithoutDelay(1))
                .Then(x => ThenTheCountIs(1))
                .BDDfy();
        }

        private void GivenConsulReturns(Service service)
        {
            _services.Add(service);
            ulong waitIndex = 0;
            _consulServiceDiscoveryProvider.Setup(x => x.RetrieveServiceListFromConsulAsync(waitIndex)).ReturnsAsync((_services.ToImmutableList(), waitIndex));
        }

        private void ThenTheCountIs(int count)
        {
            _result.Count.ShouldBe(count);
        }

        private void WhenIGetTheServices(int expected)
        {
            var result = Wait.WaitFor(3000).Until(() =>
            {
                try
                {
                    _result = _consulServiceDiscoveryProvider.Object.GetAsync().GetAwaiter().GetResult();
                    return _result.Count == expected;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            result.ShouldBeTrue();
        }

        private void WhenIGetTheServicesWithoutDelay(int expected)
        {
            bool result;
            try
            {
                _result = _consulServiceDiscoveryProvider.Object.GetAsync().GetAwaiter().GetResult();
                result = _result.Count == expected;
            }
            catch (Exception)
            {
                result = false;
            }

            result.ShouldBeTrue();
        }
    }
}
