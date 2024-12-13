using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;

public static class CrunchyrollMovieProviderServiceExtension
{
    public static void AddCrunchyrollMovieProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCrunchyrollMovieGetMetadata();
    }
}