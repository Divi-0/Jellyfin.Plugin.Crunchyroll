using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;

public static class CrunchyrollSeriesReviewsProviderServiceExtension
{
    public static void AddCrunchyrollSeriesReviewsProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICrunchyrollSeriesReviewsService, CrunchyrollSeriesReviewsService>();
    }
}