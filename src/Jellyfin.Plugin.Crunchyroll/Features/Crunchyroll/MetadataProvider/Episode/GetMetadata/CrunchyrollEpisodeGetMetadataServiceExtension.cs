using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.GetEpisodeCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.SetMetadataToEpisode;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;

public static class CrunchyrollEpisodeGetMetadataServiceExtension
{
    public static void AddCrunchyrollEpisodeGetMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetEpisodeCrunchyrollId();
        serviceCollection.AddScrapEpisodeMetadata();
        serviceCollection.AddSetMetadataToEpisode();
        serviceCollection.AddScoped<IEpisodeGetMetadataService, EpisodeGetMetadataService>();
    }
}