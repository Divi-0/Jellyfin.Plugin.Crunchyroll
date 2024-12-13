using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.ScrapLockRepository;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider;

public static class MetadataProviderServiceExtension
{
    public static void AddMetadataProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSeriesMetadataProvider();
        serviceCollection.AddSeasonMetadataProvider();
        serviceCollection.AddEpisodeMetadataProvider();
        serviceCollection.AddCrunchyrollMovieProvider();
        serviceCollection.AddScrapLockRepository();
    }
}