using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.SetMetadataToMovie;

public static class SetMetadataToMovieServiceExtension
{
    public static void AddSetMetadataToMovie(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISetMetadataToMovieRepository, SetMetadataToMovieRepository>();
        serviceCollection.AddScoped<ISetMetadataToMovieService, SetMetadataToMovieService>();
    }
}