using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public static class CrunchyrollServiceExtension
{
    public static IServiceCollection AddCrunchyroll(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>();

        serviceCollection.AddCrunchyrollLogin(configuration);
        serviceCollection.AddSearchAndAssignTitleId(configuration);
        serviceCollection.AddCrunchyrollComments(configuration);
        serviceCollection.AddCrunchyrollGetReviews(configuration);
        serviceCollection.AddCrunchyrollExtractReviews(configuration);
        serviceCollection.AddCrunchyrollAvatar(configuration);
        serviceCollection.AddCrunchyrollScrapTitleMetadata(configuration);
        serviceCollection.AddCrunchyrollGetSeasonId();
        serviceCollection.AddCrunchyrollGetEpisodeId();
        serviceCollection.AddCrunchyrollPostScanTasks();
        
        return serviceCollection;
    }
}