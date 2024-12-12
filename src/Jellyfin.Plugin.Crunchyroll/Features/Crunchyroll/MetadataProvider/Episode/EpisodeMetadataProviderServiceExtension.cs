using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode;

public static class EpisodeMetadataProviderServiceExtension
{
    public static void AddEpisodeMetadataProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCrunchyrollEpisodeGetMetadata();
        serviceCollection.AddEpisodeOverwriteParentIndexNumber();
    }
}