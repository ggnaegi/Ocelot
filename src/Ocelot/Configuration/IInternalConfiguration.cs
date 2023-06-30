using System.Collections.Generic;
using System;
using System.Net.Http;

namespace Ocelot.Configuration
{
    public interface IInternalConfiguration
    {
        List<Route> Routes { get; }

        string AdministrationPath { get; }

        ServiceProviderConfiguration ServiceProviderConfiguration { get; }

        string RequestId { get; }

        LoadBalancerOptions LoadBalancerOptions { get; }

        string DownstreamScheme { get; }

        QoSOptions QoSOptions { get; }

        HttpHandlerOptions HttpHandlerOptions { get; }

        Version DownstreamHttpVersion { get; }

        HttpVersionPolicy? DownstreamVersionPolicy { get; }
    }
}
