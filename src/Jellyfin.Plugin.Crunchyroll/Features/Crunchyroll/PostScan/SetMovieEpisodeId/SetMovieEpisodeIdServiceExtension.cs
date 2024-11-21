using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.Interfaces;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.PostScan.SetMovieEpisodeId;

public static class SetMovieEpisodeIdServiceExtension
{
    public static void AddSetMovieEpisodeId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<ICrunchyrollMovieEpisodeIdClient, CrunchyrollMovieEpisodeIdClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
        
        serviceCollection.AddSingleton<IPostMovieScanTask, SetMovieEpisodeIdTask>();
    }
}