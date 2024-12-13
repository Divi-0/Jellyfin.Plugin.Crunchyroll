using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public static class ScrapTitleMetadataServiceExtension
{
    public static IServiceCollection AddCrunchyrollScrapTitleMetadata(this IServiceCollection serviceCollection)
    {
        // serviceCollection.AddHttpClient<ICrunchyrollSeasonsClient, CrunchyrollSeasonsClient>()
        //     .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
        //     .AddPollyHttpClientDefaultPolicy();
        //
        // serviceCollection.AddHttpClient<ICrunchyrollEpisodesClient, CrunchyrollEpisodesClient>()
        //     .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
        //     .AddPollyHttpClientDefaultPolicy();
        //
        // serviceCollection.AddHttpClient<ICrunchyrollSeriesClient, CrunchyrollSeriesClient>()
        //     .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
        //     .AddPollyHttpClientDefaultPolicy();
        //
        // serviceCollection.AddScoped<IScrapTitleMetadataRepository, ScrapTitleMetadataRepository>();
        // serviceCollection.AddScoped<IGetTitleMetadataRepository, ScrapTitleMetadataRepository>();
        
        return serviceCollection;
    }
}