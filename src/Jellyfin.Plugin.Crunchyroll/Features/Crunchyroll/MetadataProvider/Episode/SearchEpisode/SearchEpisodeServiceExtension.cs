using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.SearchEpisode;

public static class SearchEpisodeServiceExtension
{
    public static void AddSearchEpisode(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISearchEpisodeService, SearchEpisodeService>();
    }
}