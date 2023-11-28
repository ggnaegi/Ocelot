using Ocelot.Configuration;
using Ocelot.Request.Middleware;
using System.IO;

namespace Ocelot.Cache;

public class CacheKeyGenerator : ICacheKeyGenerator
{
    private readonly IMemoryStreamManager _memoryStreamManager;
    private static readonly Encoding Encoding = new UTF8Encoding(false);

    public CacheKeyGenerator(IMemoryStreamManager memoryStreamManager)
    {
        _memoryStreamManager = memoryStreamManager;
    }

    public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest,
        DownstreamRoute downstreamRoute)
    {
        using var memoryStream = _memoryStreamManager.GetStream();
        await memoryStream.WriteAsync(Encoding.GetBytes(downstreamRequest.Method));
        await memoryStream.WriteAsync(Encoding.GetBytes(downstreamRequest.OriginalString));

        var cacheOptions = downstreamRoute.CacheOptions;
        if (cacheOptions == null)
        {
            memoryStream.Position = 0;
            return MD5Helper.GenerateMd5(memoryStream);
        }

        if (cacheOptions.Headers is { Length: > 0 })
        {
            await AppendHeadersValues(memoryStream, cacheOptions.Headers, downstreamRequest);
        }
        
        if (cacheOptions.RequestBodyHashing)
        {
            await AppendHashedBodyContent(memoryStream, downstreamRequest);
        }

        memoryStream.Position = 0;
        return MD5Helper.GenerateMd5(memoryStream);
    }

    private static async Task AppendHeadersValues(Stream memoryStream, IEnumerable<string> headers, DownstreamRequest downstreamRequest)
    {
        foreach (var headerKey in headers)
        {
            if (!downstreamRequest.Headers.Contains(headerKey))
            {
                continue;
            }

            var headerValue = downstreamRequest.Headers.GetValues(headerKey).FirstOrDefault();

            if (string.IsNullOrEmpty(headerValue))
            {
                continue;
            }

            await memoryStream.WriteAsync(Encoding.GetBytes(headerValue));
        }
    }

    private static async Task AppendHashedBodyContent(Stream memoryStream, DownstreamRequest downstreamRequest)
    {
        if (downstreamRequest.Content is { Headers.ContentLength: > 0 })
        {
            var contentBytes = await downstreamRequest.Content.ReadAsByteArrayAsync();
            if (contentBytes.Length > 0)
            {
                await memoryStream.WriteAsync(contentBytes);
            }
        }
    }
}
