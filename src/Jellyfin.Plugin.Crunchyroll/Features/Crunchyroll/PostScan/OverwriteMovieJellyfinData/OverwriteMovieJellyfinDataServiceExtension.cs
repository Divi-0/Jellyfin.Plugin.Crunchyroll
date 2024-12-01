using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public static class OverwriteMovieJellyfinDataServiceExtension
{
    public static void AddOverwriteMovieJellyfinData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IPostMovieIdSetTask, OverwriteMovieJellyfinDataTask>();
        serviceCollection.AddScoped<IOverwriteMovieJellyfinDataRepository, OverwriteMovieJellyfinDataRepository>();
    }
}