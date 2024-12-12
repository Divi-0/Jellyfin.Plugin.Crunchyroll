using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.Crunchyroll.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;

public class ScrapLockRepository : IScrapLockRepository
{
    private readonly IMemoryCache _memoryCache;

    private readonly object _lockObject = new object();

    public ScrapLockRepository(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }
    
    public ValueTask<bool> AddLockAsync(CrunchyrollId id)
    {
        lock (_lockObject)//TODO: write unit tests
        {
            var entry = _memoryCache.Get(id);

            //set item anyway to refresh expiration
            _memoryCache.Set(id, (byte)0, TimeSpan.FromMinutes(5));
            return ValueTask.FromResult(entry is null);
        }
    }
}