using Ocelot.Configuration.File;

namespace Ocelot.Configuration
{
    public class CacheOptions
    {
        public CacheOptions(FileCacheOptions fileCacheOptions, string region)
        {
            TtlSeconds = fileCacheOptions.TtlSeconds;
            Headers = fileCacheOptions.Headers;
            RequestBodyHashing = fileCacheOptions.RequestBodyHashing;
            Region = string.IsNullOrEmpty(fileCacheOptions.Region) ? region : fileCacheOptions.Region;
        }

        public CacheOptions(int ttlSeconds, string region, string[] headers, bool requestBodyHashing)
        {
            TtlSeconds = ttlSeconds;
            Region = region;
            Headers = headers;
            RequestBodyHashing = requestBodyHashing;
        }

        public int TtlSeconds { get; }

        public string Region { get; }

        public string[] Headers { get; }

        public bool RequestBodyHashing { get;}
    }
}
