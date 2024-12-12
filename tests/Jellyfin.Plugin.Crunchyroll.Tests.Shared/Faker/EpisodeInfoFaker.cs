using Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Crunchyroll.Tests.Shared.Faker;

public static class EpisodeInfoFaker
{
    public static EpisodeInfo Generate(Episode? episode = null)
    {
        episode ??= EpisodeFaker.Generate();

        return new EpisodeInfo
        {
            ProviderIds = episode.ProviderIds,
            Name = episode.Name,
            Path = episode.Path,
            Year = episode.ProductionYear,
            IndexNumber = episode.IndexNumber,
            IsAutomated = true,
            MetadataLanguage = episode.PreferredMetadataLanguage,
            MetadataCountryCode = episode.PreferredMetadataCountryCode,
            OriginalTitle = episode.OriginalTitle,
            PremiereDate = episode.PremiereDate,
            ParentIndexNumber = episode.ParentIndexNumber,
            SeriesProviderIds =
            {
                {CrunchyrollExternalKeys.SeriesId, CrunchyrollIdFaker.Generate()}
            },
            SeasonProviderIds = {
                {CrunchyrollExternalKeys.SeasonId, CrunchyrollIdFaker.Generate()}
            }
        };
    }
}