using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

public class CrunchyrollSessionRepository : ICrunchyrollSessionRepository
{
    private readonly IMemoryCache _memoryCache;

    public CrunchyrollSessionRepository(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }
    
    public ValueTask SetAsync(string token, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(CacheKeys.CrunchyrollSession, token, expiration ?? TimeSpan.MaxValue);

        return ValueTask.CompletedTask;
    }

    public ValueTask<string?> GetAsync(CancellationToken cancellationToken = default)
    {
        var value = _memoryCache.Get<string>(CacheKeys.CrunchyrollSession);

        return ValueTask.FromResult(value);
    }
}