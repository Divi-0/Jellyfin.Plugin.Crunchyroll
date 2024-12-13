using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.ScrapMovieMetadata;

public static class ScrapMovieMetadataServiceExtension
{
    public static void AddScrapMovieMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScrapMovieMetadataRepository, ScrapMovieMetadataRepository>();
        serviceCollection.AddScoped<IScrapMovieMetadataService, ScrapMovieMetadataService>();
    }
}