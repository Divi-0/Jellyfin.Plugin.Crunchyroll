using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.MetadataProvider.Season.ScrapSeasonMetadata.Client.Dtos;

public record CrunchyrollSeasonsResponse
{
    public IReadOnlyList<CrunchyrollSeasonsItem> Data { get; init; } = new List<CrunchyrollSeasonsItem>();
}