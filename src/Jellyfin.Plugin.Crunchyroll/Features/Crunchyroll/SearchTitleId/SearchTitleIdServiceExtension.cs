using Jellyfin.Plugin.Crunchyroll.Configuration;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId.Client;
using Jellyfin.Plugin.Crunchyroll.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.SearchTitleId;

public static class SearchTitleIdServiceExtension
{
    public static IServiceCollection AddSearchAndAssignTitleId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollTitleIdClient, CrunchyrollSeriesIdClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}