using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

internal static class OverwriteEpisodeJellyfinDataTaskServiceExtension
{
    internal static IServiceCollection AddOverwriteEpisodeJellyfinData(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IPostEpisodeIdSetTask, OverwriteEpisodeJellyfinDataTask>();
        serviceCollection.AddScoped<IOverwriteEpisodeJellyfinDataTaskRepository, OverwriteEpisodeJellyfinDataTaskRepository>();
        
        return serviceCollection;
    }
}