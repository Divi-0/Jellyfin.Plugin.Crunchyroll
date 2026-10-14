using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Common.FlareSolverr;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Search;

public static class CrunchyrollSearchServiceExtension
{
    public static void AddCrunchyrollSearch(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollSearchClient, CrunchyrollSearchClient>()
            .AddHttpMessageHandler<FlareSolverrMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
    }
}