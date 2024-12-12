using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;

public static class ScrapLockRepositoryServiceExtension
{
    public static void AddScrapLockRepository(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IScrapLockRepository, ScrapLockRepository>();
    }
}