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
        private readonly HttpRequestMessage _httpRequestMessage;

        private const string Verb = "GET";
        private const string Url = "https://some.url/blah?abcd=123";
        private const string Header = nameof(CacheKeyGeneratorTests);
        private const string HeaderName = "auth";

        public CacheKeyGeneratorTests()
        {
            var memoryStreamManager = new MemoryStreamManager();
            _cacheKeyGenerator = new CacheKeyGenerator(memoryStreamManager);

            _httpRequestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(Verb),
                RequestUri = new Uri(Url),
            };

            _httpRequestMessage.Headers.Add(HeaderName, Header);
        }

        [Fact]
        public void should_generate_cache_key_with_request_content()
        {
            CacheOptions options = new(100, "region", null, true);
            const string content = nameof(should_generate_cache_key_with_request_content);

            _httpRequestMessage.Content = new StringContent(content);

            var cacheKey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{Verb}{Url}{content}"));

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cacheKey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_without_request_content()
        {
            CacheOptions options = null;
            var bytes = Encoding.UTF8.GetBytes($"{Verb}{Url}");
            var cacheKey = MD5Helper.GenerateMd5(bytes);

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cacheKey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_with_cache_options_header()
        {
            CacheOptions options = new(100, "region", new []{HeaderName}, false);
            var cacheKey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{Verb}{Url}{Header}"));

            this.Given(x => x.GivenDownstreamRoute(options))
                .When(x => x.WhenGenerateRequestCacheKey())
                .Then(x => x.ThenGeneratedCacheKeyIs(cacheKey))
                .BDDfy();
        }

        [Fact]
        public void should_generate_cache_key_happy_path()
        {
            const string content = nameof(should_generate_cache_key_happy_path);
            _httpRequestMessage.Content = new StringContent(content);

            CacheOptions options = new(100, "region", new []{HeaderName}, true);
            var cacheKey = MD5Helper.GenerateMd5(Encoding.UTF8.GetBytes($"{Verb}{Url}{Header}{content}"));

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
            _generatedCacheKey = await _cacheKeyGenerator.GenerateRequestCacheKey(new DownstreamRequest(_httpRequestMessage), _downstreamRoute);
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
