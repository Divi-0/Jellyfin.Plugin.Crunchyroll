using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;

public static class CrunchyrollMovieReviewsProviderServiceExtension
{
    public static void AddCrunchyrollMovieReviewsProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ICrunchyrollMovieReviewsService, CrunchyrollMovieReviewsService>();
    }
}