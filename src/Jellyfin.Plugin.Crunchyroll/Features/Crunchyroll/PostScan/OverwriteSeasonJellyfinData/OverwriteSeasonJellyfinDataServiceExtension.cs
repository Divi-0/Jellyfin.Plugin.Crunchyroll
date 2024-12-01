using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeasonJellyfinData;

internal static class OverwriteSeasonJellyfinDataServiceExtension
{
    internal static IServiceCollection AddOverwriteSeasonJellyfinData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IPostSeasonIdSetTask, OverwriteSeasonJellyfinDataTask>();
        serviceCollection.AddScoped<IOverwriteSeasonJellyfinDataRepository, OverwriteSeasonJellyfinDataRepository>();
        
        return serviceCollection;
    }
}