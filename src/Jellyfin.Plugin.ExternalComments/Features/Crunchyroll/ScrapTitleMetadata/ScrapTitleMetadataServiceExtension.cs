using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Seasons;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata;

public static class ScrapTitleMetadataServiceExtension
{
    public static IServiceCollection AddCrunchyrollScrapTitleMetadata(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollSeasonsClient, CrunchyrollSeasonsClient>();
        serviceCollection.AddHttpClient<ICrunchyrollEpisodesClient, CrunchyrollEpisodesClient>();

        serviceCollection.AddSingleton<IScrapTitleMetadataSession, CrunchyrollUnitOfWork>();
        
        return serviceCollection;
    }
}