using Microsoft.Extensions.Caching.Memory;

namespace Ocelot.Cache;

public class OcelotCacheProvider : IOcelotCacheProvider
{
    private readonly CacheKeyGenerator _cacheKeyGenerator;
    private readonly OcelotMemoryCache<CachedResponse> _ocelotMemoryCache;

    public OcelotCacheProvider(IMemoryCache memoryCache)
    {
        _cacheKeyGenerator = new CacheKeyGenerator(new MemoryStreamManager());
        _ocelotMemoryCache = new OcelotMemoryCache<CachedResponse>(memoryCache);
    }

    public IOcelotCache<CachedResponse> GetResponseCache()
    {
        return _ocelotMemoryCache;
    }

    public ICacheKeyGenerator GetCacheKeyGenerator()
    {
        return _cacheKeyGenerator;
    }
}
