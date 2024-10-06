using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Series;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata;

public static class ScrapTitleMetadataServiceExtension
{
    public static IServiceCollection AddCrunchyrollScrapTitleMetadata(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollSeasonsClient, CrunchyrollSeasonsClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddHttpClient<ICrunchyrollEpisodesClient, CrunchyrollEpisodesClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddHttpClient<ICrunchyrollSeriesClient, CrunchyrollSeriesClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddSingleton<IScrapTitleMetadataSession, CrunchyrollUnitOfWork>();
        serviceCollection.AddSingleton<IGetTitleMetadata, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}