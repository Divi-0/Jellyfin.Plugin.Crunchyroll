using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.SetMetadataToSeries;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.Reviews;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series;

public static class SeriesMetadataProviderServiceExtension
{
    public static void AddSeriesMetadataProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddGetSeriesCrunchyrollId();
        serviceCollection.AddScrapSeriesMetadata();
        serviceCollection.AddSetMetadataToSeries();
        serviceCollection.AddScoped<ISeriesGetMetadataService, SeriesGetMetadataService>();

        serviceCollection.AddCrunchyrollSeriesReviewsProvider();
    }
}