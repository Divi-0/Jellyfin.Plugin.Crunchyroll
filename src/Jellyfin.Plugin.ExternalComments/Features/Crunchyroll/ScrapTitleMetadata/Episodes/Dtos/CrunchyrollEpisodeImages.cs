using System.Collections.Generic;

namespace Jellyfin.Plugin.ExternalComments.Features.Crunchyroll.ScrapTitleMetadata.Episodes.Dtos;

public record CrunchyrollEpisodeImages
{
    public IReadOnlyList<CrunchyrollEpisodeThumbnail> Thumbnail { get; init; } = new List<CrunchyrollEpisodeThumbnail>();
}