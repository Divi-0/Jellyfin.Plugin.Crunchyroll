using System.Collections.Generic;

namespace Jellyfin.Plugin.Crunchyroll.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

public record CrunchyrollSeasonsResponse
{
    public IReadOnlyList<CrunchyrollSeasonsItem> Data { get; init; } = new List<CrunchyrollSeasonsItem>();
}