using System;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.Reviews.ExtractReviews;

public static class CrunchyrollExtractReviewsServiceExtension
{
    public static IServiceCollection AddCrunchyrollExtractReviews(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<IHtmlReviewsExtractor, HtmlReviewsExtractor>(httpclient =>
            {
                httpclient.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddScoped<IAddReviewsRepistory, ReviewsRepistory>();
        
        return serviceCollection;
    }
}