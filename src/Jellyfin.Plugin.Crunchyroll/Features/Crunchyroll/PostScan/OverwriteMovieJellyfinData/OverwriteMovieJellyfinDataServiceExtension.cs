using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteMovieJellyfinData;

public static class OverwriteMovieJellyfinDataServiceExtension
{
    public static void AddOverwriteMovieJellyfinData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPostMovieIdSetTask, OverwriteMovieJellyfinDataTask>();
        serviceCollection.AddScoped<IOverwriteMovieJellyfinDataUnitOfWork, CrunchyrollUnitOfWork>();
    }
}