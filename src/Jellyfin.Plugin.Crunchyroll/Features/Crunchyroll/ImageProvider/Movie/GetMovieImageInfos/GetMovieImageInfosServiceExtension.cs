using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.ImageProvider.Movie.GetMovieImageInfos;

public static class GetMovieImageInfosServiceExtension
{
    public static void AddGetMovieImageInfos(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetMovieImageInfosRepository, GetMovieImageInfosRepository>();
        serviceCollection.AddScoped<IGetMovieImageInfosService, GetMovieImageInfosService>();
    }
}