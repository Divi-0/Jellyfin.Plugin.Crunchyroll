using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetSeriesCrunchyrollId;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SetMetadataToSeries;
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
    }
}