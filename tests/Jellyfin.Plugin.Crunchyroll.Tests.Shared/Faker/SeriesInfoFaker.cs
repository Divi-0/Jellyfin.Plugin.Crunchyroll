using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class SeriesInfoFaker
{
    public static SeriesInfo Generate(Series? series = null)
    {
        series ??= SeriesFaker.Generate();

        return new SeriesInfo
        {
            ProviderIds = series.ProviderIds,
            Name = series.Name,
            Path = series.Path,
            Year = series.ProductionYear,
            IndexNumber = series.IndexNumber,
            IsAutomated = true,
            MetadataLanguage = series.PreferredMetadataLanguage,
            MetadataCountryCode = series.PreferredMetadataCountryCode,
            OriginalTitle = series.OriginalTitle,
            PremiereDate = series.PremiereDate,
            ParentIndexNumber = series.ParentIndexNumber
        };
    }
}