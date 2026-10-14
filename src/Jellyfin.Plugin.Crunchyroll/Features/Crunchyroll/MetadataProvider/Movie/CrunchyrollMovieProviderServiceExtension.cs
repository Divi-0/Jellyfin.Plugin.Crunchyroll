using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Comments;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.GetMetadata;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.Reviews;
using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie.SearchMovie;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Movie;

public static class CrunchyrollMovieProviderServiceExtension
{
    public static void AddCrunchyrollMovieProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCrunchyrollMovieGetMetadata();
        serviceCollection.AddSearchMovie();
        serviceCollection.AddCrunchyrollMovieReviewsProvider();
        serviceCollection.AddCrunchyrollMovieCommentsProvider();
    }
}