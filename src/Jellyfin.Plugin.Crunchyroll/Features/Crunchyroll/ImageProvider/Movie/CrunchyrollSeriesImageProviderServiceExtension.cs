using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie;

public static class CrunchyrollMovieImageProviderServiceExtension
{
    public static void AddCrunchyrollMovieImageProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetMovieImageInfos();
    }
}