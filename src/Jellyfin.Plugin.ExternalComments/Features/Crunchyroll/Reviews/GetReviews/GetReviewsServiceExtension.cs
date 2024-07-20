using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.GetReviews;

public static class GetReviewsServiceExtension
{
    public static IServiceCollection AddCrunchyrollGetReviews(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollGetReviewsClient, CrunchyrollGetReviewsClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddSingleton<IGetReviewsSession, ReviewsUnitOfWork>();
        
        return serviceCollection;
    }
}