using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Episode;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider;

public static class ImageProviderServiceExtension
{
    public static void AddImageProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCrunchyrollSeriesImageProvider();
        serviceCollection.AddCrunchyrollEpisodeImageProvider();
        serviceCollection.AddCrunchyrollMovieImageProvider();
    }
}