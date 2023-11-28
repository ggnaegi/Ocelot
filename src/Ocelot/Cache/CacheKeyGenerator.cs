using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache;

public class CacheKeyGenerator : ICacheKeyGenerator
{
    private const char Delimiter = '-';
    private readonly IMemoryStreamManager _memoryStreamManager;

    public CacheKeyGenerator(IMemoryStreamManager memoryStreamManager)
    {
        _memoryStreamManager = memoryStreamManager;
    }

    public async ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest,
        DownstreamRoute downstreamRoute)
    {
        using var memoryStream = _memoryStreamManager.GetStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

        await writer.WriteAsync(downstreamRequest.Method);
        await writer.WriteAsync(Delimiter);
        await writer.WriteAsync(downstreamRequest.OriginalString);

        var cacheOptions = downstreamRoute.CacheOptions;
        if (cacheOptions == null)
        {
            return await GenerateMd5(writer, memoryStream);
        }

        await AppendHeadersValues(writer, cacheOptions.Headers, downstreamRequest);
        await AppendHashedBodyContent(writer, cacheOptions.RequestBodyHashing, downstreamRequest);
        return await GenerateMd5(writer, memoryStream);
    }

    private static async Task<string> GenerateMd5(StreamWriter writer, MemoryStream memoryStream)
    {
        await writer.FlushAsync();
        memoryStream.Position = 0;
        return MD5Helper.GenerateMd5(memoryStream.ToArray());
    }

    private static async Task AppendHeadersValues(StreamWriter writer, IReadOnlyCollection<string> headers,
        DownstreamRequest downstreamRequest)
    {
        if (headers == null || headers.Count == 0)
        {
            return;
        }

        foreach (var headerKey in headers)
        {
            var header = downstreamRequest.Headers
                .FirstOrDefault(r => r.Key.Equals(headerKey, StringComparison.OrdinalIgnoreCase))
                .Value?.FirstOrDefault();

            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            await writer.WriteAsync(Delimiter);
            await writer.WriteAsync(header);
        }
    }

    private static async Task AppendHashedBodyContent(StreamWriter writer, bool requestBodyHashing,
        DownstreamRequest downstreamRequest)
    {
        if (!requestBodyHashing)
        {
            return;
        }

        var contentBytes = await downstreamRequest.Content.ReadAsByteArrayAsync();
        await writer.WriteAsync(Delimiter);
        await writer.WriteAsync(Convert.ToBase64String(contentBytes));
    }
}
