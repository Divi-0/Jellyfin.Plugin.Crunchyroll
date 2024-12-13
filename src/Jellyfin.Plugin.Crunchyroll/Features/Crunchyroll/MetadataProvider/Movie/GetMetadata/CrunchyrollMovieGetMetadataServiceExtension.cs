using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;

public static class CrunchyrollMovieGetMetadataServiceExtension
{
    public static void AddCrunchyrollMovieGetMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetMovieCrunchyrollId();
        serviceCollection.AddScrapMovieMetadata();
        serviceCollection.AddSetMetadataToMovie();
        serviceCollection.AddScoped<ICrunchyrollMovieGetMetadataService, CrunchyrollMovieGetMetadataService>();
    }
}