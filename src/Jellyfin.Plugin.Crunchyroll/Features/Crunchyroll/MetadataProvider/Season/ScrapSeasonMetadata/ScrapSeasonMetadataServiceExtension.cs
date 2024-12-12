using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata;

public static class ScrapSeasonMetadataServiceExtension
{
    public static void AddScrapSeasonMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScrapSeasonMetadataService, ScrapSeasonMetadataService>();
        serviceCollection.AddScoped<IScrapSeasonMetadataRepository, ScrapSeasonMetadataRepository>();
        serviceCollection.AddHttpClient<ICrunchyrollSeasonsClient, CrunchyrollSeasonsClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
    }
}