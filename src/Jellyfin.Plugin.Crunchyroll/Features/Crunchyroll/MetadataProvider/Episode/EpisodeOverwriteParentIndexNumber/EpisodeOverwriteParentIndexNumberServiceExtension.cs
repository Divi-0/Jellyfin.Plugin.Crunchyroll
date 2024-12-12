using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.EpisodeOverwriteParentIndexNumber;

public static class EpisodeOverwriteParentIndexNumberServiceExtension
{
    public static void AddEpisodeOverwriteParentIndexNumber(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IEpisodeOverwriteParentIndexNumberService, EpisodeOverwriteParentIndexNumberService>();
    }
}