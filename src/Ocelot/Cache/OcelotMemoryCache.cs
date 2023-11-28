using Microsoft.Extensions.Caching.Memory;

namespace Ocelot.Cache
{
    public class OcelotMemoryCache<T> : IOcelotCache<T>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Dictionary<string, List<string>> _regions;

        public OcelotMemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _regions = new();
        }

        public void Add(string key, T value, TimeSpan ttl, string region)
        {
            if (ttl.TotalMilliseconds <= 0)
            {
                return;
            }

            _memoryCache.Set(key, value, ttl);

            SetRegion(region, key);
        }

        public T Get(string key, string region)
        {
            return _memoryCache.TryGetValue(key, out T value) ? value : default;
        }

        public void ClearRegion(string region)
        {
            if (!_regions.ContainsKey(region))
            {
                return;
            }

            var keys = _regions[region];
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }
        }

        public void AddAndDelete(string key, T value, TimeSpan ttl, string region)
        {
            if (_memoryCache.TryGetValue(key, out T _))
            {
                _memoryCache.Remove(key);
            }

            Add(key, value, ttl, region);
        }

        private void SetRegion(string region, string key)
        {
            if (_regions.TryGetValue(region, out var current))
            {
                if (!current.Contains(key))
                {
                    current.Add(key);
                }
            }
            else
            {
                _regions.Add(region, new List<string> { key });
            }
        }
    }
}
