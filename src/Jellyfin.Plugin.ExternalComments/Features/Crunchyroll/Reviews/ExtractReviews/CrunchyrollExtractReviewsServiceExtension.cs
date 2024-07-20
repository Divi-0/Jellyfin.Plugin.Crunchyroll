using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ExtractReviews;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.Reviews.ExtractReviews;

public static class CrunchyrollExtractReviewsServiceExtension
{
    public static IServiceCollection AddCrunchyrollExtractReviews(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<IHtmlReviewsExtractor, HtmlReviewsExtractor>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<IAddReviewsSession, ReviewsUnitOfWork>();
        
        return serviceCollection;
    }
}