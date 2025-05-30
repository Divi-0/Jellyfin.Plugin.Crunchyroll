using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Series.GetMetadata.GetSeriesCrunchyrollId;

public static class GetSeriesCrunchyrollIdServiceExtension
{
    public static void AddGetSeriesCrunchyrollId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetSeriesCrunchyrollIdService, GetSeriesCrunchyrollIdService>();
        serviceCollection.AddHttpClient<ICrunchyrollSeriesIdClient, CrunchyrollSeriesIdClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
    }
}