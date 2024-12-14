using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;

public static class ICrunchyrollMovieCommentsProviderServiceExtension
{
    public static void AddCrunchyrollMovieCommentsProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICrunchyrollMovieCommentsService, CrunchyrollMovieCommentsService>();
    }
}