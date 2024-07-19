using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews.Client;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.GetReviews;

public static class GetReviewsServiceExtension
{
    public static IServiceCollection AddCrunchyrollGetReviews(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollGetReviewsClient, CrunchyrollGetReviewsClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddSingleton<IGetReviewsSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}