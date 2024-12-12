using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.DeleteTitleMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Login;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetEpisodeThumbnail;
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
    public static IServiceCollection AddCrunchyroll(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>();
        serviceCollection.AddTransient<HttpUserAgentHeaderMessageHandler>();
        
        serviceCollection.AddCrunchyrollLogin();
        serviceCollection.AddSearchAndAssignTitleId();
        serviceCollection.AddCrunchyrollComments();
        serviceCollection.AddCrunchyrollGetReviews();
        serviceCollection.AddCrunchyrollExtractReviews();
        serviceCollection.AddCrunchyrollAvatar();
        serviceCollection.AddCrunchyrollScrapTitleMetadata();
        serviceCollection.AddCrunchyrollGetSeasonId();
        serviceCollection.AddCrunchyrollGetEpisodeId();
        serviceCollection.AddCrunchyrollPostScanTasks();
        serviceCollection.AddSetEpisodeThumbnail();
        serviceCollection.AddDeleteTitleMetadata();
        
        serviceCollection.AddMetadataProvider();
        
        return serviceCollection;
    }
}