using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ocelot.Cache
{
    [Authorize]
    [Route("outputcache")]
    public class OutputCacheController : Controller
    {
        private readonly IOcelotCacheProvider _cacheProvider;

        public OutputCacheController(IOcelotCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        [HttpDelete]
        [Route("{region}")]
        public IActionResult Delete(string region)
        {
            _cacheProvider.GetResponseCache().ClearRegion(region);
            return new NoContentResult();
        }
    }
}
