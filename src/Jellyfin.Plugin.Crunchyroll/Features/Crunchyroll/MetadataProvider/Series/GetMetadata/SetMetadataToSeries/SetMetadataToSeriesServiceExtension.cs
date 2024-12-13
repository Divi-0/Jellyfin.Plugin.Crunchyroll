using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.SetMetadataToSeries;

public static class SetMetadataToSeriesServiceExtension
{
    public static void AddSetMetadataToSeries(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISetMetadataToSeriesService, SetMetadataToSeriesService>();
        serviceCollection.AddScoped<ISetMetadataToSeriesRepository, SetMetadataToSeriesRepository>();
    }
}