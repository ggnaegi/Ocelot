namespace Ocelot.Cache;

public interface IOcelotCacheProvider
{
    IOcelotCache<CachedResponse> GetResponseCache();
    ICacheKeyGenerator GetCacheKeyGenerator();
}
