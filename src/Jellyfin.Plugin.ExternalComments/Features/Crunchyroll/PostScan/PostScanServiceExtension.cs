using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan
{
    internal static class PostScanServiceExtension
    {
        public static IServiceCollection AddCrunchyrollPostScanTasks(this IServiceCollection serviceCollection, PluginConfiguration configuration)
        {
            serviceCollection.AddSingleton<IPostScanTask, SetTitleIdTask>();
            
            serviceCollection.AddSingleton<IPostTitleIdSetTask, ScrapTitleMetadataTask>();
            serviceCollection.AddSingleton<IPostTitleIdSetTask, SetSeasonIdTask>();
            
            serviceCollection.AddSingleton<IPostSeasonIdSetTask, SetEpisodeIdTask>();

            return serviceCollection;
        }
    }
}
