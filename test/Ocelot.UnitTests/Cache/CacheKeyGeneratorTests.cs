using Ocelot.Cache;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Request.Middleware;
using System.Net.Http.Headers;
using System.Text;

namespace Ocelot.UnitTests.Cache
{
    public class CacheKeyGeneratorTests
    {
        private readonly ICacheKeyGenerator _cacheKeyGenerator;
        private readonly Mock<HttpRequestMessage> _httpRequestMessage;

        private const string verb = "GET";
        private const string url = "https://some.url/blah?abcd=123";
        private const string header = nameof(CacheKeyGeneratorTests);
        private const string headerName = "auth";

        public CacheKeyGeneratorTests()
        {
            var memoryStreamManager = new MemoryStreamManager();
            _cacheKeyGenerator = new CacheKeyGenerator(memoryStreamManager);

            _httpRequestMessage = new Mock<HttpRequestMessage>();
            _httpRequestMessage.SetupGet(x => x.Method).Returns(new HttpMethod(verb));
            _httpRequestMessage.SetupGet(x => x.RequestUri.OriginalString).Returns(url);

            var headers = new HttpHeadersStub
            {
                { headerName, header },
            };



            _httpRequestMessage.SetupGet(x => x.Headers).Returns(new HttpRequestHeaders(){});
        }

        [Fact]
        public void should_generate_cache_key_with_request_content()
        {
            const string content = nameof(should_generate_cache_key_with_request_content);

            _downstreamRequest.SetupGet(x => x.HasContent).Returns(true);
            _downstreamRequest.Setup(x => x.ReadContentAsync()).ReturnsAsync(content);

            var cachekey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{verb}-{url}-{content}"));

            this.Given(x => x.GivenDownstreamRoute(null))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_without_request_content()
        {
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(false);

            CacheOptions options = null;
            var cachekey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{verb}-{url}"));

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cachekey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_with_cache_options_header()
        {
            _downstreamRequest.SetupGet(x => x.HasContent).Returns(false);

            CacheOptions options = new(100, "region", new []{headerName}, false);
            var cacheKey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{verb}-{url}-{header}"));

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cacheKey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_happy_path()
        {
            const string content = nameof(should_generate_cache_key_happy_path);

            _downstreamRequest.SetupGet(x => x.HasContent).Returns(true);
            _downstreamRequest.Setup(x => x.ReadContentAsync()).ReturnsAsync(content);

            CacheOptions options = new(100, "region", new []{headerName}, true);
            var cacheKey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{verb}-{url}-{header}-{content}"));

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cacheKey))
                .BDDfy();
        }

        private DownstreamRoute _downstreamRoute;

        private void GivenDownstreamRoute(CacheOptions options)
        {
            _downstreamRoute = new DownstreamRouteBuilder()
                .WithKey("key1")
                .WithCacheOptions(options)
                .Build();
        }

        private string _generatedCacheKey;

        private async Task WhenGenerateRequestCacheKey()
        {
            _generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(_downstreamRequest.Object, _downstreamRoute);
        }

        private void ThenGeneratedCacheKeyIs(string expected)
        {
            _generatedCacheKey.ShouldBe(expected);
        }
    }

    internal class HttpHeadersStub : HttpHeaders
    {
        public HttpHeadersStub() : base() { }
    }
}
