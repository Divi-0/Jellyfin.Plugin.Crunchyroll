using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class MovieInfoFaker
{
    public static MovieInfo Generate(Movie? movie = null)
    {
        movie ??= MovieFaker.Generate();

        return new MovieInfo
        {
            ProviderIds = movie.ProviderIds,
            Name = movie.Name,
            Path = movie.Path,
            Year = movie.ProductionYear,
            IndexNumber = movie.IndexNumber,
            IsAutomated = true,
            MetadataLanguage = movie.PreferredMetadataLanguage,
            MetadataCountryCode = movie.PreferredMetadataCountryCode,
            OriginalTitle = movie.OriginalTitle,
            PremiereDate = movie.PremiereDate,
            ParentIndexNumber = movie.ParentIndexNumber
        };
    }
}