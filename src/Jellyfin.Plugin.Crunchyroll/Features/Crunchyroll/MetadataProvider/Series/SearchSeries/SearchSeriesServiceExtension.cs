using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.SearchSeries;

public static class SearchSeriesServiceExtension
{
    public static void AddSearchSeries(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISearchSeriesService, SearchSeriesService>();
    }
}