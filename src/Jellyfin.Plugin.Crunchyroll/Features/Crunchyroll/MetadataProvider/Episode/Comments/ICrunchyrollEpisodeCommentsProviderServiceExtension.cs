using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Episode.Comments;

public static class ICrunchyrollEpisodeCommentsProviderServiceExtension
{
    public static void AddCrunchyrollEpisodeCommentsProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICrunchyrollEpisodeCommentsService, CrunchyrollEpisodeCommentsService>();
    }
}