using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.ScrapSeriesMetadata;

public static class ScrapSeriesMetadataServiceExtension
{
    public static void AddScrapSeriesMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScrapSeriesMetadataService, ScrapSeriesMetadataService>();
        serviceCollection.AddScoped<IScrapSeriesMetadataRepository, ScrapSeriesMetadataRepository>();
        serviceCollection.AddHttpClient<ICrunchyrollSeriesClient, CrunchyrollSeriesClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
    }
}