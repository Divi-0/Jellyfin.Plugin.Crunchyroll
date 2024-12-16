using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.Client;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata.ScrapMissingEpisode;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.GetMetadata.ScrapEpisodeMetadata;

public static class ScrapEpisodeMetadataServiceExtension
{
    public static void AddScrapEpisodeMetadata(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScrapEpisodeMetadataRepository, ScrapEpisodeMetadataRepository>();
        serviceCollection.AddScoped<IScrapEpisodeMetadataService, ScrapEpisodeMetadataService>();
        serviceCollection.AddHttpClient<IScrapEpisodeCrunchyrollClient, ScrapEpisodeCrunchyrollClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();

        serviceCollection.AddScrapMissingEpisode();
    }
}