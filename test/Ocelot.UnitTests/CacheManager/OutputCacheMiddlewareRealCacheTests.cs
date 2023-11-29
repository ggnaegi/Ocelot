using CacheManager.Core;
using Microsoft.AspNetCore.Http;
using Ocelot.Cache;
using Ocelot.Cache.CacheManager;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Net.Http.Headers;

namespace Ocelot.UnitTests.CacheManager
{
    public class OutputCacheMiddlewareRealCacheTests
    {
        private readonly IOcelotCache<CachedResponse> _cacheManager;
        private readonly OutputCacheMiddleware _middleware;
        private readonly HttpContext _httpContext;
        private readonly Mock<IOcelotCacheProvider> _cacheProvider;

        public OutputCacheMiddlewareRealCacheTests()
        {
            _httpContext = new DefaultHttpContext();
            var loggerFactory = new Mock<IOcelotLoggerFactory>();
            var logger = new Mock<IOcelotLogger>();
            loggerFactory.Setup(x => x.CreateLogger<OutputCacheMiddleware>()).Returns(logger.Object);
            var cacheManagerOutputCache = CacheFactory.Build<CachedResponse>("OcelotOutputCache", x =>
            {
                x.WithDictionaryHandle();
            });
            _cacheManager = new OcelotCacheManagerCache<CachedResponse>(cacheManagerOutputCache);
            ICacheKeyGenerator cacheKeyGenerator = new CacheKeyGenerator(new MemoryStreamManager());
            _httpContext.Items.UpsertDownstreamRequest(new Ocelot.Request.Middleware.DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "https://some.url/blah?abcd=123")));
            _cacheProvider = new Mock<IOcelotCacheProvider>();
            _cacheProvider.Setup(x => x.GetCacheKeyGenerator()).Returns(cacheKeyGenerator);
            _cacheProvider.Setup(x => x.GetResponseCache()).Returns(_cacheManager);

            static Task Next(HttpContext context) => Task.CompletedTask;
            _middleware = new OutputCacheMiddleware(Next, loggerFactory.Object, _cacheProvider.Object);
        }

        [Fact]
        public void should_cache_content_headers()
        {
            var content = new StringContent("{\"Test\": 1}")
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") },
            };

            var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "fooreason");

            this.Given(x => x.GivenResponseIsNotCached(response))
                .And(x => x.GivenTheDownstreamRouteIs())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheContentTypeHeaderIsCached())
                .BDDfy();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_httpContext).GetAwaiter().GetResult();
        }

        private void ThenTheContentTypeHeaderIsCached()
        {
            var cacheKey = MD5Helper.GenerateMd5("GEThttps://some.url/blah?abcd=123"u8.ToArray());
            var result = _cacheManager.Get(cacheKey, "kanken");
            var header = result.ContentHeaders["Content-Type"];
            header.First().ShouldBe("application/json");
        }

        private void GivenResponseIsNotCached(DownstreamResponse response)
        {
            _httpContext.Items.UpsertDownstreamResponse(response);
        }

        private void GivenTheDownstreamRouteIs()
        {
            var route = new DownstreamRouteBuilder()
                .WithIsCached(true)
                .WithCacheOptions(new CacheOptions(100, "kanken", null, false))
                .WithUpstreamHttpMethod(new List<string> { "Get" })
                .Build();

            _httpContext.Items.UpsertDownstreamRoute(route);
        }
    }
}
