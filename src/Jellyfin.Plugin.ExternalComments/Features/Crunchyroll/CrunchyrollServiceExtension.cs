using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetComments;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Login;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;
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
        
        return serviceCollection;
    }
}