using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.OverwriteEpisodeJellyfinData;

internal static class OverwriteEpisodeJellyfinDataTaskServiceExtension
{
    internal static IServiceCollection AddOverwriteEpisodeJellyfinDataTask(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPostEpisodeIdSetTask, OverwriteEpisodeJellyfinDataTask>();
        serviceCollection.AddSingleton<IOverwriteEpisodeJellyfinDataTaskSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}