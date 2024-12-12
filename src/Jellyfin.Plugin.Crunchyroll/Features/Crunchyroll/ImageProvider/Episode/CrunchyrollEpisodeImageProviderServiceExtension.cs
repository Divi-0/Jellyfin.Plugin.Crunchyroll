using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode.GetEpisodeImageInfos;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode;

public static class CrunchyrollEpisodeImageProviderServiceExtension
{
    public static void AddCrunchyrollEpisodeImageProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetEpisodeImageInfos();
    }
}