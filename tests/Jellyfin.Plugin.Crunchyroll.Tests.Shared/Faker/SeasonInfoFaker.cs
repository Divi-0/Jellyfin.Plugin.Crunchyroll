using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class SeasonInfoFaker
{
    public static SeasonInfo Generate(Season? season = null)
    {
        season ??= SeasonFaker.Generate();

        return new SeasonInfo
        {
            ProviderIds = season.ProviderIds,
            Name = season.Name,
            Path = season.Path,
            Year = season.ProductionYear,
            IndexNumber = season.IndexNumber,
            IsAutomated = true,
            MetadataLanguage = season.PreferredMetadataLanguage,
            MetadataCountryCode = season.PreferredMetadataCountryCode,
            OriginalTitle = season.OriginalTitle,
            PremiereDate = season.PremiereDate,
            ParentIndexNumber = season.ParentIndexNumber,
            SeriesProviderIds =
            {
                {CrunchyrollExternalKeys.SeriesId, CrunchyrollIdFaker.Generate()}
            }
        };
    }
}