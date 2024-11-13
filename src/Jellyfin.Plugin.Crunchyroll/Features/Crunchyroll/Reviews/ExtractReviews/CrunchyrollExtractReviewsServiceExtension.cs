using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public static class CrunchyrollExtractReviewsServiceExtension
{
    public static IServiceCollection AddCrunchyrollExtractReviews(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IHtmlReviewsExtractor, HtmlReviewsExtractor>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<IAddReviewsSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}