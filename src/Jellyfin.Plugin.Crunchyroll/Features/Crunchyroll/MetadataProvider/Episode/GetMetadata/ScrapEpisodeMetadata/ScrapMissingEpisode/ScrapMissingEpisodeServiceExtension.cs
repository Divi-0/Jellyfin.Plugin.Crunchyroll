using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;

public static class ScrapMissingEpisodeServiceExtension
{
    public static void AddScrapMissingEpisode(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScrapMissingEpisodeRepository, ScrapMissingEpisodeRepository>();
        serviceCollection.AddScoped<IScrapMissingEpisodeService, ScrapMissingEpisodeService>();
    }
}