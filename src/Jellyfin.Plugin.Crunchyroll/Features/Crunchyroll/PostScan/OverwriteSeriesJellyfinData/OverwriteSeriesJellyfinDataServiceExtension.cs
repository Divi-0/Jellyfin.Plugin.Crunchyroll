using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteSeriesJellyfinData;

internal static class OverwriteSeriesJellyfinDataServiceExtension
{
    internal static IServiceCollection AddOverwriteSeriesJellyfinDataTask(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IPostTitleIdSetTask, OverwriteSeriesJellyfinDataTask>();
        
        return serviceCollection;
    }
}