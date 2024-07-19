using Jellyfin.Plugin.ExternalComments.Common;
using Jellyfin.Plugin.ExternalComments.Configuration;
using Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.SearchAndAssignTitleId;

public static class SearchAndAssignTitleIdServiceExtension
{
    public static IServiceCollection AddSearchAndAssignTitleId(this IServiceCollection serviceCollection, PluginConfiguration configuration)
    {
        serviceCollection.AddHttpClient<ICrunchyrollTitleIdClient, CrunchyrollTitleIdClient>()
            .AddFlareSolverrProxy(configuration)
            .AddPollyHttpClientDefaultPolicy();
        
        return serviceCollection;
    }
}