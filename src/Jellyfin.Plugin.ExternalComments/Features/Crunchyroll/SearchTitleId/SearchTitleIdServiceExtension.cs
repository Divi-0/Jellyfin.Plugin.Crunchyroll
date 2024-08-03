using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchTitleId;

public static class SearchTitleIdServiceExtension
{
    public static IServiceCollection AddSearchAndAssignTitleId(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollTitleIdClient, CrunchyrollTitleIdClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}