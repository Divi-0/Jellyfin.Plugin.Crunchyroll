using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;

public static class SearchMovieServiceExtension
{
    public static void AddSearchMovie(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISearchMovieService, SearchMovieService>();
    }
}