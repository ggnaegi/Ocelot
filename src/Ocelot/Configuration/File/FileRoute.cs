namespace Ocelot.Configuration.File
{
    public class FileRoute : IRoute
    {
        public FileRoute()
        {
            AddClaimsToRequest = new Dictionary<string, string>();
            AddHeadersToRequest = new Dictionary<string, string>();
            AddQueriesToRequest = new Dictionary<string, string>();
            AuthenticationOptions = new FileAuthenticationOptions();
            ChangeDownstreamPathTemplate = new Dictionary<string, string>();
            DelegatingHandlers = new List<string>();
            DownstreamHeaderTransform = new Dictionary<string, string>();
            DownstreamHostAndPorts = new List<FileHostAndPort>();
            FileCacheOptions = new FileCacheOptions();
            HttpHandlerOptions = new FileHttpHandlerOptions();
            LoadBalancerOptions = new FileLoadBalancerOptions();
            Priority = 1;
            QoSOptions = new FileQoSOptions();
            RateLimitOptions = new FileRateLimitRule();
            RouteClaimsRequirement = new Dictionary<string, string>();
            SecurityOptions = new FileSecurityOptions();
            UpstreamHeaderTransform = new Dictionary<string, string>();
            UpstreamHttpMethod = new List<string>();
        }

        public Dictionary<string, string> AddClaimsToRequest { get; set; }
        public Dictionary<string, string> AddHeadersToRequest { get; set; }
        public Dictionary<string, string> AddQueriesToRequest { get; set; }
        public FileAuthenticationOptions AuthenticationOptions { get; set; }
        public Dictionary<string, string> ChangeDownstreamPathTemplate { get; set; }
        public bool DangerousAcceptAnyServerCertificateValidator { get; set; }
        public List<string> DelegatingHandlers { get; set; }
        public Dictionary<string, string> DownstreamHeaderTransform { get; set; }
        public List<FileHostAndPort> DownstreamHostAndPorts { get; set; }
        public string DownstreamHttpVersion { get; set; }
        public string DownstreamHttpMethod { get; set; }
        public string DownstreamPathTemplate { get; set; }
        public string DownstreamScheme { get; set; }
        public string DownstreamVersionPolicy { get; set; }
        public FileCacheOptions FileCacheOptions { get; set; }
        public FileHttpHandlerOptions HttpHandlerOptions { get; set; }
        public string Key { get; set; }
        public FileLoadBalancerOptions LoadBalancerOptions { get; set; }
        public int Priority { get; set; }
        public FileQoSOptions QoSOptions { get; set; }
        public FileRateLimitRule RateLimitOptions { get; set; }
        public string RequestIdKey { get; set; }
        public Dictionary<string, string> RouteClaimsRequirement { get; set; }
        public bool RouteIsCaseSensitive { get; set; }
        public FileSecurityOptions SecurityOptions { get; set; }
        public string ServiceName { get; set; }
        public string ServiceNamespace { get; set; }
        public int Timeout { get; set; }
        public Dictionary<string, string> UpstreamHeaderTransform { get; set; }
        public string UpstreamHost { get; set; }
        public List<string> UpstreamHttpMethod { get; set; }
        public string UpstreamPathTemplate { get; set; }
    }
}
