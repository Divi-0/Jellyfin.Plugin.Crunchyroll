using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.TitleMetadata.ScrapTitleMetadata.Seasons.Dtos;

public record CrunchyrollSeasonsResponse
{
    public IReadOnlyList<CrunchyrollSeasonsItem> Data { get; init; } = new List<CrunchyrollSeasonsItem>();
}