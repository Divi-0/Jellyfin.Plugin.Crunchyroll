using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;

internal static class PostScanServiceExtension
{
    public static IServiceCollection AddCrunchyrollPostScanTasks(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IPostScanTask, SetTitleIdTask>();
            
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ScrapTitleMetadataTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, SetSeasonIdTask>();
        serviceCollection.AddSingleton<IPostTitleIdSetTask, ExtractReviewsTask>();
            
        serviceCollection.AddSingleton<IPostSeasonIdSetTask, SetEpisodeIdTask>();
        
        serviceCollection.AddSingleton<IPostEpisodeIdSetTask, ExtractCommentsTask>();

        return serviceCollection;
    }
}