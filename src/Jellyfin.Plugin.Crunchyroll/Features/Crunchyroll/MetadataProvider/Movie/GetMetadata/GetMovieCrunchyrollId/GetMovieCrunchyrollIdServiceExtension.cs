using Jellyfin.Plugin.Crunchyroll.Common;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata.GetMovieCrunchyrollId;

public static class GetMovieCrunchyrollIdServiceExtension
{
    public static void AddGetMovieCrunchyrollId(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IGetMovieCrunchyrollIdService, GetMovieCrunchyrollIdService>();
        serviceCollection.AddHttpClient<ICrunchyrollMovieEpisodeIdClient, CrunchyrollMovieEpisodeIdClient>()
            .AddHttpMessageHandler<HttpUserAgentHeaderMessageHandler>()
            .AddPollyHttpClientDefaultPolicy();
    }
}