using Microsoft.AspNetCore.Mvc;
using Ocelot.Cache;

namespace Ocelot.UnitTests.Controllers
{
    public class OutputCacheControllerTests
    {
        private readonly OutputCacheController _controller;
        private readonly Mock<IOcelotCacheProvider> _cacheProvider;
        private IActionResult _result;

        public OutputCacheControllerTests()
        {
            _cacheProvider = new Mock<IOcelotCacheProvider>();
            _cacheProvider.Setup(x => x.GetResponseCache())
                .Returns(new Mock<IOcelotCache<CachedResponse>>().Object);
            _controller = new OutputCacheController(_cacheProvider.Object);
        }

        [Fact]
        public void should_delete_key()
        {
            this.When(_ => WhenIDeleteTheKey("a"))
               .Then(_ => ThenTheKeyIsDeleted("a"))
               .BDDfy();
        }

        private void ThenTheKeyIsDeleted(string key)
        {
            _result.ShouldBeOfType<NoContentResult>();
            _cacheProvider
                .Verify(x => x.GetResponseCache().ClearRegion(key), Times.Once);
        }

        private void WhenIDeleteTheKey(string key)
        {
            _result = _controller.Delete(key);
        }
    }
}
