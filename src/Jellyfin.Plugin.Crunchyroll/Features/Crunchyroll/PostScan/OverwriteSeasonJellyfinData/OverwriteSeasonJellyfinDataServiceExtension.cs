using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

internal static class OverwriteSeasonJellyfinDataServiceExtension
{
    internal static IServiceCollection AddOverwriteSeasonJellyfinData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPostSeasonIdSetTask, OverwriteSeasonJellyfinDataTask>();
        serviceCollection.AddSingleton<IOverwriteSeasonJellyfinDataSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}