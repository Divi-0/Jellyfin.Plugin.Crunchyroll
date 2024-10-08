using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.GetReviews;

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