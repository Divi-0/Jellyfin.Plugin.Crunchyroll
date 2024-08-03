using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodeThumbnail
{
    public IReadOnlyList<CrunchyrollEpisodeThumbnailSizes> Sizes { get; init; } = new List<CrunchyrollEpisodeThumbnailSizes>();
}