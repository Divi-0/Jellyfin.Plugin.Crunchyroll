using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Avatar;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Comments.GetComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll;

public static class CrunchyrollServiceExtension
{
    public static IServiceCollection AddCrunchyroll(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddSingleton<ICrunchyrollSessionRepository, CrunchyrollSessionRepository>();

        serviceCollection.AddCrunchyrollLogin(configuration);
        serviceCollection.AddSearchAndAssignTitleId(configuration);
        serviceCollection.AddCrunchyrollGetComments(configuration);
        serviceCollection.AddCrunchyrollGetReviews(configuration);
        serviceCollection.AddCrunchyrollExtractReviews(configuration);
        serviceCollection.AddCrunchyrollAvatar(configuration);
        serviceCollection.AddCrunchyrollScrapTitleMetadata(configuration);
        
        return serviceCollection;
    }
}