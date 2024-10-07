using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetEpisodeId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.GetSeasonId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;

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